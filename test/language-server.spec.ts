import * as Backend from "backend";
import * as vscode from "vscode-languageserver/browser";
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
jest.spyOn(vscode, "createConnection").mockImplementation((reader, writer) => {
    connection = createConnectionOriginal(reader, writer);
    connection.sendDiagnostics = jest.fn();
    connection.onDidOpenTextDocument = jest.fn();
    connection.onDidCloseTextDocument = jest.fn();
    connection.onDidChangeTextDocument = jest.fn();
    connection.onCompletion = jest.fn();
    connection.onDocumentSymbol = jest.fn();
    connection.onRequest = jest.fn();
    connection.onHover = jest.fn();
    connection.onFoldingRanges = jest.fn();
    return connection;
});

beforeEach(() => {
    Backend.Language.bootServer = jest.fn();
    Backend.SettingsHandler.configure = jest.fn();
    Backend.MetadataHandler.updateMetadata = jest.fn();
    Backend.HoverHandler.hover = jest.fn();
    Backend.CompletionHandler.complete = jest.fn();
    Backend.TokenHandler.getTokens = jest.fn();
    Backend.TokenHandler.getAllTokens = jest.fn();
    Backend.SymbolHandler.getSymbols = jest.fn();
    Backend.DocumentHandler.upsertDocuments = jest.fn();
    Backend.DocumentHandler.changeDocument = jest.fn();
    Backend.DocumentHandler.removeDocument = jest.fn();
    Backend.FoldingHandler.getFoldingRanges = jest.fn();
    Backend.TokenHandler.getTokenLegend = jest.fn();
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
    Backend.DiagnosticPublisher.publishDiagnostics("foo", []);
    expect(connection.sendDiagnostics).toBeCalledWith({ diagnostics: [], uri: "foo" });
});

test("open document handler is routed", () => {
    jest.mocked(connection.onDidOpenTextDocument).mock.calls[0][0]({
        textDocument: { uri: "foo", text: "bar", version: 0, languageId: "" }
    });
    expect(Backend.DocumentHandler.upsertDocuments).toBeCalledWith([{ uri: "foo", text: "bar" }]);
});

test("change document handler is routed", () => {
    jest.mocked(connection.onDidChangeTextDocument).mock.calls[0][0]({
        textDocument: { uri: "foo", version: 0 },
        contentChanges: []
    });
    expect(Backend.DocumentHandler.changeDocument).toBeCalledWith("foo", []);
});

test("completion handler is routed", () => {
    jest.mocked(connection.onCompletion).mock.calls[0][0]({
        textDocument: { uri: "foo" },
        position: { line: 1, character: 2 }
    }, {} as never, {} as never);
    expect(Backend.CompletionHandler.complete).toBeCalledWith("foo", { line: 1, character: 2 });
});

test("symbol handler is routed", () => {
    jest.mocked(connection.onDocumentSymbol).mock.calls[0][0]({
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
    jest.mocked(connection.onHover).mock.calls[0][0]({
        textDocument: { uri: "foo" },
        position: { line: 1, character: 2 }
    }, {} as never, {} as never);
    expect(Backend.HoverHandler.hover).toBeCalledWith("foo", { line: 1, character: 2 });
});

test("folding handler is routed", () => {
    jest.mocked(connection.onFoldingRanges).mock.calls[0][0]({
        textDocument: { uri: "foo" }
    }, {} as never, {} as never);
    expect(Backend.FoldingHandler.getFoldingRanges).toBeCalledWith("foo");
});

function simulateCustomRequest(callId: number, params: object) {
    jest.mocked(connection.onRequest).mock.calls[callId].at(1)?.(params as never, {} as never, {} as never);
}
