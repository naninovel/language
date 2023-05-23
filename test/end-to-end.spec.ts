import * as Backend from "backend";
import * as LSP from "vscode-languageserver-protocol";
import { Emitter, createMessageConnection } from "vscode-languageserver";
import { bootLanguageServer, configure, languageId, LanguageMessageReader, LanguageMessageWriter } from "../src";

const serverReader = new Emitter<LSP.NotificationMessage>();
const serverWriter = new Emitter<LSP.NotificationMessage>();
const clientReader = new LanguageMessageReader(serverWriter);
const clientWriter = new LanguageMessageWriter(serverReader);
const out: LSP.Message[] = [];
const connection = createMessageConnection(clientReader, clientWriter);
clientReader.listen(e => out.push(e));

it("can boot", async () => {
    await Backend.boot();
    expect(() => bootLanguageServer(serverReader, serverWriter)).not.toThrow();
    connection.listen();
});

it("can provide symbols", async () => {
    await connection.sendNotification(LSP.DidOpenTextDocumentNotification.type,
        { textDocument: { uri: "symbols.nani", text: "# label", languageId, version: 0 } });
    const items = await connection.sendRequest(LSP.DocumentSymbolRequest.type,
        { textDocument: { uri: "symbols.nani" } });
    expect((items as LSP.DocumentSymbol[]).length).toBeGreaterThan(0);
});

it("can autocomplete", async () => {
    await connection.sendNotification(LSP.DidOpenTextDocumentNotification.type,
        { textDocument: { uri: "complete.nani", text: "@", languageId, version: 0 } });
    const items = await connection.sendRequest(LSP.CompletionRequest.type,
        { textDocument: { uri: "complete.nani" }, position: { line: 0, character: 1 } });
    expect((items as LSP.CompletionItem[]).length).toBeGreaterThan(0);
});

it("can publish diagnostics", async () => {
    configure({ diagnoseSyntax: true, diagnoseSemantics: false, diagnoseNavigation: false });
    await connection.sendNotification(LSP.DidOpenTextDocumentNotification.type,
        { textDocument: { uri: "diag.nani", text: "@", languageId, version: 0 } });
    const diags = ((out.pop() as LSP.NotificationMessage).params as LSP.PublishDiagnosticsParams).diagnostics;
    expect(diags[0].message).toEqual("Command identifier is missing.");
});
