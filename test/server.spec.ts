import * as cs from "backend";
import * as ls from "vscode-languageserver/browser";
import { expect, test, beforeEach, vi } from "vitest";
import { Emitter, Message, WorkspaceEdit, FileChangeType } from "vscode-languageserver";
import { bootLanguageServer, applyCustomMetadata, upsertDocuments, configure, LanguageMessageReader, LanguageMessageWriter } from "../src";
import { createConfiguration } from "../src/configuration";
import { mergeMetadata, getDefaultMetadata } from "@naninovel/common";

class MockMessage implements Message {
    constructor(public jsonrpc: string) {}
}

const reader = new Emitter<Message>();
const writer = new Emitter<Message>();
let connection: ls.Connection;

const createConnectionOriginal = ls.createConnection;
vi.spyOn(ls, "createConnection").mockImplementation((reader, writer) => {
    connection = createConnectionOriginal(reader, writer);
    connection.sendDiagnostics = vi.fn();
    connection.onDidOpenTextDocument = vi.fn();
    connection.onDidChangeTextDocument = vi.fn();
    connection.workspace.onDidRenameFiles = vi.fn();
    connection.workspace.onDidDeleteFiles = vi.fn();
    connection.workspace.applyEdit = vi.fn();
    connection.onDidChangeWatchedFiles = vi.fn();
    connection.onCompletion = vi.fn();
    connection.onDocumentSymbol = vi.fn();
    connection.onRequest = vi.fn();
    connection.onHover = vi.fn();
    connection.onFoldingRanges = vi.fn();
    connection.onRenameRequest = vi.fn();
    return connection;
});

beforeEach(() => {
    cs.Language.bootServer = vi.fn();
    cs.Configurator.configure = vi.fn();
    cs.MetadataUpdater.updateMetadata = vi.fn();
    cs.HoverHandler.hover = vi.fn();
    cs.CompletionHandler.complete = vi.fn();
    cs.TokenHandler.getTokens = vi.fn();
    cs.TokenHandler.getAllTokens = vi.fn();
    cs.SymbolHandler.getSymbols = vi.fn();
    cs.DocumentHandler.upsertDocuments = vi.fn();
    cs.DocumentHandler.renameDocuments = vi.fn();
    cs.DocumentHandler.deleteDocuments = vi.fn();
    cs.DocumentHandler.changeDocument = vi.fn();
    cs.FoldingHandler.getFoldingRanges = vi.fn();
    cs.TokenHandler.getTokenLegend = vi.fn();
    cs.RenameHandler.rename = vi.fn();
});

// Custom request listeners attached via `connection.onRequest(...)`;
// mapped value corresponds to the attachment order (listener index).
const customRequests = {
    semanticTokensFull: 0,
    semanticTokensRange: 1
};

test("after booting server metadata is updated with default values", () => {
    expect(() => bootLanguageServer(reader, writer)).not.toThrow();
    expect(cs.Language.bootServer).toBeCalled();
    expect(cs.MetadataUpdater.updateMetadata).toBeCalledWith(getDefaultMetadata());
});

test("reader can read messages", async () => {
    let resolve: (value: Message) => void = null as never;
    const promise = new Promise<Message>(r => resolve = r);
    new LanguageMessageReader(reader).listen(resolve);
    reader.fire(new MockMessage("foo"));
    expect((await promise).jsonrpc).toEqual("foo");
});

test("writer can write messages", async () => {
    let resolve: (value: Message) => void = null as never;
    const promise = new Promise<Message>(r => resolve = r);
    writer.event(resolve);
    const client = new LanguageMessageWriter(writer);
    await client.write(new MockMessage("foo"));
    client.end();
    expect((await promise).jsonrpc).toEqual("foo");
});

test("can create configuration", () => {
    expect(createConfiguration()).not.toBeNull();
});

test("when configured settings handler is invoked", () => {
    const settings = { diagnoseSyntax: true, diagnoseSemantics: true, diagnoseNavigation: false };
    configure(settings as never);
    expect(cs.Configurator.configure).toBeCalledWith(settings);
});

test("when upsert document handler is invoked", () => {
    upsertDocuments([{ uri: "foo", text: "bar" }]);
    expect(cs.DocumentHandler.upsertDocuments).toBeCalledWith([{ uri: "foo", text: "bar" }]);
});

test("when applying custom metadata update metadata is invoked", () => {
    const custom = { variables: ["foo"] } as cs.Metadata.Project;
    const expectedMerged = mergeMetadata(getDefaultMetadata(), custom);
    applyCustomMetadata(custom);
    expect(cs.MetadataUpdater.updateMetadata).toBeCalledWith(expectedMerged);
});

test("publish diagnostics on backend routes to send diagnostics", () => {
    cs.DiagnosticPublisher.publishDiagnostics("foo", []);
    expect(connection.sendDiagnostics).toBeCalledWith({ uri: "foo", diagnostics: [] });
});

test("publish edits on backend routes to apply workspace edits", () => {
    cs.EditPublisher.publishEdit("label", {
        documentChanges: [{
            textDocument: "doc.nani",
            edits: [{
                range: { start: { line: 0, character: 0 }, end: { line: 0, character: 0 } },
                newText: "foo"
            }]
        }]
    });
    expect(connection.workspace.applyEdit).toBeCalledWith({
        label: "label", edit: {
            documentChanges: [{
                textDocument: { uri: "doc.nani", version: null },
                edits: [{
                    range: { start: { line: 0, character: 0 }, end: { line: 0, character: 0 } },
                    newText: "foo"
                }]
            }]
        } satisfies WorkspaceEdit
    });
});

test("open document handler is routed", () => {
    vi.mocked(connection.onDidOpenTextDocument).mock.calls[0][0]({
        textDocument: { uri: "foo.nani", text: "bar", version: 0, languageId: "" }
    });
    expect(cs.DocumentHandler.upsertDocuments).toBeCalledWith([{ uri: "foo.nani", text: "bar" }]);
});

test("change document handler is routed", () => {
    vi.mocked(connection.onDidChangeTextDocument).mock.calls[0][0]({
        textDocument: { uri: "foo.nani", version: 0 },
        contentChanges: []
    });
    expect(cs.DocumentHandler.changeDocument).toBeCalledWith("foo.nani", []);
});

test("rename documents handler is routed", () => {
    vi.mocked(connection.workspace.onDidRenameFiles).mock.calls[0][0]({
        files: [{ oldUri: "foo.nani", newUri: "bar.nani" }]
    });
    expect(cs.DocumentHandler.renameDocuments).toBeCalledWith([{ oldUri: "foo.nani", newUri: "bar.nani" }]);
});

test("delete documents handler is routed", () => {
    vi.mocked(connection.workspace.onDidDeleteFiles).mock.calls[0][0]({
        files: [{ uri: "foo.nani" }]
    });
    expect(cs.DocumentHandler.deleteDocuments).toBeCalledWith([{ uri: "foo.nani" }]);
});

test("rename folder file events routed to rename document", () => {
    // This is just to catch folder renames, as `workspace/didRenameFiles` is not firing on these.
    // As per the LSP protocol `workspace/didRenameFiles` should actually cover folders as well,
    // but vscode only send those on file renames, hence the hack.
    // When renaming a folder, vscode notifies with 2 events: first creates and the other deletes.
    vi.mocked(connection.onDidChangeWatchedFiles).mock.calls[0][0]({
        changes: [
            { uri: "/bar", type: FileChangeType.Created },
            { uri: "/foo", type: FileChangeType.Deleted }
        ]
    });
    expect(cs.DocumentHandler.renameDocuments).toBeCalledWith([{ oldUri: "/foo", newUri: "/bar" }]);
});

test("other file events are not routed to rename document", () => {
    vi.mocked(connection.onDidChangeWatchedFiles).mock.calls[0][0]({ changes: [] });
    vi.mocked(connection.onDidChangeWatchedFiles).mock.calls[0][0]({
        changes: [{ uri: "/bar", type: FileChangeType.Created }]
    });
    vi.mocked(connection.onDidChangeWatchedFiles).mock.calls[0][0]({
        changes: [
            { uri: "/bar.nani", type: FileChangeType.Created },
            { uri: "/foo.nani", type: FileChangeType.Deleted }]
    });
    vi.mocked(connection.onDidChangeWatchedFiles).mock.calls[0][0]({
        changes: [
            { uri: "/bar", type: FileChangeType.Created },
            { uri: "/foo", type: FileChangeType.Created }]
    });
    expect(cs.DocumentHandler.renameDocuments).not.toBeCalled();
});

test("completion handler is routed", () => {
    vi.mocked(connection.onCompletion).mock.calls[0][0]({
        textDocument: { uri: "foo" },
        position: { line: 1, character: 2 }
    }, {} as never, {} as never);
    expect(cs.CompletionHandler.complete).toBeCalledWith("foo", { line: 1, character: 2 });
});

test("symbol handler is routed", () => {
    vi.mocked(connection.onDocumentSymbol).mock.calls[0][0]({
        textDocument: { uri: "foo" }
    }, {} as never, {} as never);
    expect(cs.SymbolHandler.getSymbols).toBeCalledWith("foo");
});

test("semantic full handler is routed", () => {
    simulateCustomRequest(customRequests.semanticTokensFull, { textDocument: { uri: "foo" } });
    expect(cs.TokenHandler.getAllTokens).toBeCalledWith("foo");
});

test("semantic range handler is routed", () => {
    simulateCustomRequest(customRequests.semanticTokensRange, { textDocument: { uri: "foo" }, range: {} });
    expect(cs.TokenHandler.getTokens).toBeCalledWith("foo", {});
});

test("hover handler is routed", () => {
    vi.mocked(connection.onHover).mock.calls[0][0]({
        textDocument: { uri: "foo" },
        position: { line: 1, character: 2 }
    }, {} as never, {} as never);
    expect(cs.HoverHandler.hover).toBeCalledWith("foo", { line: 1, character: 2 });
});

test("folding handler is routed", () => {
    vi.mocked(connection.onFoldingRanges).mock.calls[0][0]({
        textDocument: { uri: "foo" }
    }, {} as never, {} as never);
    expect(cs.FoldingHandler.getFoldingRanges).toBeCalledWith("foo");
});

test("rename handler is routed", () => {
    vi.mocked(connection.onRenameRequest).mock.calls[0][0]({
        textDocument: { uri: "foo" },
        position: { line: 1, character: 2 },
        newName: "bar"
    }, {} as never, {} as never);
    expect(cs.RenameHandler.rename).toBeCalledWith("foo", { line: 1, character: 2 }, "bar");
});

function simulateCustomRequest(callId: number, params: object) {
    vi.mocked(connection.onRequest).mock.calls[callId].at(1)?.(params as never, {} as never, {} as never);
}
