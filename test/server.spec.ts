import * as Backend from "backend";
import * as vscode from "vscode-languageserver/browser";
import { expect, test, beforeEach, vi } from "vitest";
import { Emitter, Message } from "vscode-languageserver";
import { bootLanguageServer, applyCustomMetadata, upsertDocuments, configure, LanguageMessageReader, LanguageMessageWriter } from "../src";
import { createConfiguration } from "../src/configuration";
import { mergeMetadata, getDefaultMetadata } from "@naninovel/common";

class MockMessage implements Message {
    constructor(public jsonrpc: string) {}
}

const reader = new Emitter<Message>();
const writer = new Emitter<Message>();
let connection: vscode.Connection;

const createConnectionOriginal = vscode.createConnection;
vi.spyOn(vscode, "createConnection").mockImplementation((reader, writer) => {
    connection = createConnectionOriginal(reader, writer);
    connection.sendDiagnostics = vi.fn();
    connection.onDidOpenTextDocument = vi.fn();
    connection.onDidChangeTextDocument = vi.fn();
    connection.workspace.onDidRenameFiles = vi.fn();
    connection.workspace.onDidDeleteFiles = vi.fn();
    connection.onCompletion = vi.fn();
    connection.onDocumentSymbol = vi.fn();
    connection.onRequest = vi.fn<[]>();
    connection.onHover = vi.fn();
    connection.onFoldingRanges = vi.fn();
    return connection;
});

beforeEach(() => {
    Backend.Language.bootServer = vi.fn();
    Backend.SettingsHandler.configure = vi.fn();
    Backend.MetadataHandler.updateMetadata = vi.fn();
    Backend.HoverHandler.hover = vi.fn();
    Backend.CompletionHandler.complete = vi.fn();
    Backend.TokenHandler.getTokens = vi.fn();
    Backend.TokenHandler.getAllTokens = vi.fn();
    Backend.SymbolHandler.getSymbols = vi.fn();
    Backend.DocumentHandler.upsertDocuments = vi.fn();
    Backend.DocumentHandler.renameDocuments = vi.fn();
    Backend.DocumentHandler.deleteDocuments = vi.fn();
    Backend.DocumentHandler.changeDocument = vi.fn();
    Backend.FoldingHandler.getFoldingRanges = vi.fn();
    Backend.TokenHandler.getTokenLegend = vi.fn();
});

test("after booting server metadata is updated with default values", () => {
    expect(() => bootLanguageServer(reader, writer)).not.toThrow();
    expect(Backend.Language.bootServer).toBeCalled();
    expect(Backend.MetadataHandler.updateMetadata).toBeCalledWith(getDefaultMetadata());
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
    configure(settings);
    expect(Backend.SettingsHandler.configure).toBeCalledWith(settings);
});

test("when upsert document handler is invoked", () => {
    upsertDocuments([{ uri: "foo", text: "bar" }]);
    expect(Backend.DocumentHandler.upsertDocuments).toBeCalledWith([{ uri: "foo", text: "bar" }]);
});

test("when applying custom metadata update metadata is invoked", () => {
    const custom = { variables: ["foo"] } as Backend.Metadata.Project;
    const expectedMerged = mergeMetadata(getDefaultMetadata(), custom);
    applyCustomMetadata(custom);
    expect(Backend.MetadataHandler.updateMetadata).toBeCalledWith(expectedMerged);
});

test("publish diagnostics on backend routes to send diagnostics", () => {
    // @ts-ignore
    Backend.DiagnosticPublisher.$publishDiagnostics("foo", []);
    expect(connection.sendDiagnostics).toBeCalledWith({ uri: "foo", diagnostics: [] });
});

test("open document handler is routed", () => {
    vi.mocked(connection.onDidOpenTextDocument).mock.calls[0][0]({
        textDocument: { uri: "foo", text: "bar", version: 0, languageId: "" }
    });
    expect(Backend.DocumentHandler.upsertDocuments).toBeCalledWith([{ uri: "foo", text: "bar" }]);
});

test("change document handler is routed", () => {
    vi.mocked(connection.onDidChangeTextDocument).mock.calls[0][0]({
        textDocument: { uri: "foo", version: 0 },
        contentChanges: []
    });
    expect(Backend.DocumentHandler.changeDocument).toBeCalledWith("foo", []);
});

test("rename documents handler is routed", () => {
    vi.mocked(connection.workspace.onDidRenameFiles).mock.calls[0][0]({
        files: [{ oldUri: "foo", newUri: "bar" }]
    });
    expect(Backend.DocumentHandler.renameDocuments).toBeCalledWith([{ oldUri: "foo", newUri: "bar" }]);
});

test("delete documents handler is routed", () => {
    vi.mocked(connection.workspace.onDidDeleteFiles).mock.calls[0][0]({
        files: [{ uri: "foo" }]
    });
    expect(Backend.DocumentHandler.deleteDocuments).toBeCalledWith([{ uri: "foo" }]);
});

test("completion handler is routed", () => {
    vi.mocked(connection.onCompletion).mock.calls[0][0]({
        textDocument: { uri: "foo" },
        position: { line: 1, character: 2 }
    }, {} as never, {} as never);
    expect(Backend.CompletionHandler.complete).toBeCalledWith("foo", { line: 1, character: 2 });
});

test("symbol handler is routed", () => {
    vi.mocked(connection.onDocumentSymbol).mock.calls[0][0]({
        textDocument: { uri: "foo" }
    }, {} as never, {} as never);
    expect(Backend.SymbolHandler.getSymbols).toBeCalledWith("foo");
});

test("semantic full handler is routed", () => {
    simulateCustomRequest(0, { textDocument: { uri: "foo" } });
    expect(Backend.TokenHandler.getAllTokens).toBeCalledWith("foo");
});

test("semantic range handler is routed", () => {
    simulateCustomRequest(1, { textDocument: { uri: "foo" }, range: {} });
    expect(Backend.TokenHandler.getTokens).toBeCalledWith("foo", {});
});

test("hover handler is routed", () => {
    vi.mocked(connection.onHover).mock.calls[0][0]({
        textDocument: { uri: "foo" },
        position: { line: 1, character: 2 }
    }, {} as never, {} as never);
    expect(Backend.HoverHandler.hover).toBeCalledWith("foo", { line: 1, character: 2 });
});

test("folding handler is routed", () => {
    vi.mocked(connection.onFoldingRanges).mock.calls[0][0]({
        textDocument: { uri: "foo" }
    }, {} as never, {} as never);
    expect(Backend.FoldingHandler.getFoldingRanges).toBeCalledWith("foo");
});

function simulateCustomRequest(callId: number, params: object) {
    vi.mocked(connection.onRequest).mock.calls[callId].at(1)?.(params as never, {} as never, {} as never);
}
