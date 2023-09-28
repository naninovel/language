import * as Backend from "backend";
import * as LSP from "vscode-languageserver-protocol";
import { expect, test, beforeEach, beforeAll } from "vitest";
import { Emitter, createMessageConnection } from "vscode-languageserver";
import { bootLanguageServer, configure, languageId, LanguageMessageReader, LanguageMessageWriter } from "../src";

const serverReader = new Emitter<LSP.NotificationMessage>();
const serverWriter = new Emitter<LSP.NotificationMessage>();
const clientReader = new LanguageMessageReader(serverWriter);
const clientWriter = new LanguageMessageWriter(serverReader);
const testFileUri = "file:\\\\dir\\test.nani";
const out: LSP.Message[] = [];
const connection = createMessageConnection(clientReader, clientWriter);

beforeAll(async () => { await Backend.default.boot(); });
beforeEach(() => { out.length = 0; });

test("can boot", async () => {
    expect(() => bootLanguageServer(serverReader, serverWriter)).not.toThrow();
    clientReader.listen(e => out.push(e));
    connection.listen();
});

test("can be configured", async () => {
    expect(() => configure({ diagnoseSyntax: true, diagnoseSemantics: true, diagnoseNavigation: true })).not.toThrow();
});

test("can provide symbols", async () => {
    await openScript("");
    const items = await connection.sendRequest(LSP.DocumentSymbolRequest.type,
        { textDocument: { uri: testFileUri } });
    expect((items as LSP.DocumentSymbol[]).length).toBeGreaterThan(0);
});

test("symbol kind is number", async () => {
    await openScript("");
    const items = await connection.sendRequest(LSP.DocumentSymbolRequest.type,
        { textDocument: { uri: testFileUri } });
    expect(typeof (items as LSP.DocumentSymbol[])[0].kind === "number");
});

test("can delete files", async () => {
    await openScript("");
    await connection.sendNotification(LSP.DidDeleteFilesNotification.type,
        { files: [{ uri: testFileUri }] });
    await expect(connection.sendRequest(LSP.DocumentSymbolRequest.type,
        { textDocument: { uri: testFileUri } })).rejects.toThrow();
});

test("can rename files", async () => {
    await openScript("");
    await connection.sendNotification(LSP.DidRenameFilesNotification.type,
        { files: [{ oldUri: testFileUri, newUri: "new.nani" }] });
    const items = await connection.sendRequest(LSP.DocumentSymbolRequest.type,
        { textDocument: { uri: "new.nani" } });
    expect((items as LSP.DocumentSymbol[]).length).toBeGreaterThan(0);
});

test("can autocomplete", async () => {
    await openScript("@");
    const items = await connection.sendRequest(LSP.CompletionRequest.type,
        { textDocument: { uri: testFileUri }, position: { line: 0, character: 1 } });
    expect((items as LSP.CompletionItem[]).length).toBeGreaterThan(0);
});

test("can publish diagnostics", async () => {
    await openScript("# label");
    expect(() => configure({ diagnoseSyntax: true, diagnoseSemantics: true, diagnoseNavigation: true })).not.toThrow();
    const params = peekOut<LSP.PublishDiagnosticsParams>(LSP.PublishDiagnosticsNotification.method);
    expect(params.uri).toEqual(testFileUri);
    expect(params.diagnostics[0].message).toEqual("Unused label.");
    expect(params.diagnostics[0].severity).toEqual(LSP.DiagnosticSeverity.Information);
});

async function openScript(text: string) {
    await connection.sendNotification(LSP.DidOpenTextDocumentNotification.type,
        { textDocument: { uri: testFileUri, text, languageId, version: 0 } });
    while (out.length === 0) // wait for backend to diagnose and post back
        await new Promise(f => setTimeout(f, 1));
}

function peekOut<TParams>(method: string): TParams {
    for (let i = out.length - 1; i >= 0; i--)
        if ((out[i] as LSP.NotificationMessage).method === method)
            return (out[i] as LSP.NotificationMessage).params as TParams;
    throw Error("Failed to find requested server message.");
}
