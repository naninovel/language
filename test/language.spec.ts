import { Language, Metadata } from "backend";
import { Emitter, Message } from "vscode-languageserver";
import { bootLanguageServer, applyCustomMetadata, loadScriptDocument, LanguageMessageReader, LanguageMessageWriter } from "../src";
import { createConfiguration } from "../src/configuration";
import { mergeMetadata, getDefaultMetadata } from "@naninovel/common";
import * as vscode from "vscode-languageserver/browser";
import openDocument = Language.openDocument;

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
    Language.createHandlers = jest.fn();
    Language.hover = jest.fn();
    Language.complete = jest.fn();
    Language.getTokens = jest.fn();
    Language.getAllTokens = jest.fn();
    Language.getSymbols = jest.fn();
    Language.openDocument = jest.fn();
    Language.changeDocument = jest.fn();
    Language.closeDocument = jest.fn();
    Language.getFoldingRanges = jest.fn();
    Language.getTokenLegend = jest.fn();
});

test("language server can boot", () => {
    expect(() => bootLanguageServer(reader, writer)).not.toThrow();
});

test("reader can read messages", async () => {
    let resolve, promise = new Promise<Message>(r => resolve = r);
    new LanguageMessageReader(reader).listen(resolve);
    reader.fire(new MockMessage("foo"));
    expect((await promise).jsonrpc).toEqual("foo");
});

test("writer can write messages", async () => {
    let resolve: any, promise = new Promise<Message>(r => resolve = r);
    writer.event(resolve);
    const client = new LanguageMessageWriter(writer);
    await client.write(new MockMessage("foo"));
    client.end();
    expect((await promise).jsonrpc).toEqual("foo");
});

test("can create configuration", () => {
    expect(createConfiguration()).not.toBeNull();
});

test("when loading script document open handler is invoked", () => {
    loadScriptDocument("foo", "bar");
    expect(Language.openDocument).toBeCalledWith("foo", "bar");
});

test("when applying custom metadata handlers are re-created with merged meta", () => {
    const custom = { variables: ["foo"] } as Metadata.Project;
    const expectedMerged = mergeMetadata(getDefaultMetadata(), custom);
    applyCustomMetadata(custom);
    expect(Language.createHandlers).toBeCalledWith(expectedMerged);
});

test("publish diagnostics on backend routes to send diagnostics", () => {
    Language.publishDiagnostics("foo", []);
    expect(connection.sendDiagnostics).toBeCalledWith({ diagnostics: [], uri: "foo" });
});

test("open document handler is routed", () => {
    jest.mocked(connection.onDidOpenTextDocument).mock.calls[0][0]({
        textDocument: { uri: "foo", text: "bar", version: 0, languageId: "" }
    });
    expect(Language.openDocument).toBeCalledWith("foo", "bar");
});

test("change document handler is routed", () => {
    jest.mocked(connection.onDidChangeTextDocument).mock.calls[0][0]({
        textDocument: { uri: "foo", version: 0 },
        contentChanges: []
    });
    expect(Language.changeDocument).toBeCalledWith("foo", []);
});

test("completion handler is routed", () => {
    jest.mocked(connection.onCompletion).mock.calls[0][0]({
        textDocument: { uri: "foo" },
        position: { line: 1, character: 2 }
    }, {} as never, {} as never);
    expect(Language.complete).toBeCalledWith("foo", { line: 1, character: 2 });
});

test("symbol handler is routed", () => {
    jest.mocked(connection.onDocumentSymbol).mock.calls[0][0]({
        textDocument: { uri: "foo" }
    }, {} as never, {} as never);
    expect(Language.getSymbols).toBeCalledWith("foo");
});

test("semantic full handler is routed", () => {
    // @ts-ignore
    jest.mocked(connection.onRequest).mock.calls[0][1]({ textDocument: { uri: "foo" } });
    expect(Language.getAllTokens).toBeCalledWith("foo");
});

test("semantic range handler is routed", () => {
    // @ts-ignore
    jest.mocked(connection.onRequest).mock.calls[1][1]({ textDocument: { uri: "foo" }, range: {} });
    expect(Language.getTokens).toBeCalledWith("foo", {});
});

test("hover handler is routed", () => {
    jest.mocked(connection.onHover).mock.calls[0][0]({
        textDocument: { uri: "foo" },
        position: { line: 1, character: 2 }
    }, {} as never, {} as never);
    expect(Language.hover).toBeCalledWith("foo", { line: 1, character: 2 });
});

test("folding handler is routed", () => {
    jest.mocked(connection.onFoldingRanges).mock.calls[0][0]({
        textDocument: { uri: "foo" }
    }, {} as never, {} as never);
    expect(Language.getFoldingRanges).toBeCalledWith("foo");
});
