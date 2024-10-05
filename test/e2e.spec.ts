import * as lsp from "vscode-languageserver-protocol";
import backend from "backend";
import { expect, test, beforeEach, beforeAll } from "vitest";
import { Emitter, createMessageConnection } from "vscode-languageserver";
import { bootLanguageServer, configure, languageId, LanguageMessageReader, LanguageMessageWriter } from "../src";

const serverReader = new Emitter<lsp.NotificationMessage>();
const serverWriter = new Emitter<lsp.NotificationMessage>();
const clientReader = new LanguageMessageReader(serverWriter);
const clientWriter = new LanguageMessageWriter(serverReader);
const testFileUri = "file:\\\\dir\\test.nani";
const out: lsp.Message[] = [];
const connection = createMessageConnection(clientReader, clientWriter);

beforeAll(async () => { await backend.boot(); });
beforeEach(() => { out.length = 0; });

test("can boot", async () => {
    expect(() => bootLanguageServer(serverReader, serverWriter)).not.toThrow();
    clientReader.listen(e => out.push(e));
    connection.listen();
});

test("can be configured", async () => {
    expect(() => configure({
        scenarioRoot: "foo",
        debounceDelay: 100,
        diagnoseSyntax: true,
        diagnoseSemantics: true,
        diagnoseNavigation: true,
        refactorFileRenames: true
    })).not.toThrow();
});

test("can provide symbols", async () => {
    await openScript("");
    const items = await connection.sendRequest(lsp.DocumentSymbolRequest.type,
        { textDocument: { uri: testFileUri } });
    expect((items as lsp.DocumentSymbol[]).length).toBeGreaterThan(0);
});

test("symbol kind is number", async () => {
    await openScript("");
    const items = await connection.sendRequest(lsp.DocumentSymbolRequest.type,
        { textDocument: { uri: testFileUri } });
    expect(typeof (items as lsp.DocumentSymbol[])[0].kind === "number");
});

test("can delete files", async () => {
    await openScript("");
    await connection.sendNotification(lsp.DidDeleteFilesNotification.type,
        { files: [{ uri: testFileUri }] });
    await expect(connection.sendRequest(lsp.DocumentSymbolRequest.type,
        { textDocument: { uri: testFileUri } })).rejects.toThrow();
});

test("can rename files", async () => {
    await openScript("");
    await connection.sendNotification(lsp.DidRenameFilesNotification.type,
        { files: [{ oldUri: testFileUri, newUri: "new.nani" }] });
    const items = await connection.sendRequest(lsp.DocumentSymbolRequest.type,
        { textDocument: { uri: "new.nani" } });
    expect((items as lsp.DocumentSymbol[]).length).toBeGreaterThan(0);
});

test("can autocomplete", async () => {
    await openScript("@");
    const items = await connection.sendRequest(lsp.CompletionRequest.type,
        { textDocument: { uri: testFileUri }, position: { line: 0, character: 1 } });
    expect((items as lsp.CompletionItem[]).length).toBeGreaterThan(0);
});

test("can publish diagnostics", async () => {
    await openScript("# label");
    expect(() => configure({
        scenarioRoot: "foo",
        debounceDelay: 100,
        diagnoseSyntax: true,
        diagnoseSemantics: true,
        diagnoseNavigation: true,
        refactorFileRenames: true
    })).not.toThrow();
    const params = peekOut<lsp.PublishDiagnosticsParams>(lsp.PublishDiagnosticsNotification.method);
    expect(params.uri).toEqual(testFileUri);
    expect(params.diagnostics[0].message).toEqual("Unused label.");
    expect(params.diagnostics[0].severity).toEqual(lsp.DiagnosticSeverity.Information);
});

async function openScript(text: string) {
    await connection.sendNotification(lsp.DidOpenTextDocumentNotification.type,
        { textDocument: { uri: testFileUri, text, languageId, version: 0 } });
    while (out.length === 0) // wait for backend to diagnose and post back
        await new Promise(f => setTimeout(f, 1));
}

function peekOut<TParams>(method: string): TParams {
    for (let i = out.length - 1; i >= 0; i--)
        if ((out[i] as lsp.NotificationMessage).method === method)
            return (out[i] as lsp.NotificationMessage).params as TParams;
    throw Error("Failed to find requested server message.");
}
