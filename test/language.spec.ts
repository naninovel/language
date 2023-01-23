import { Language, Metadata } from "backend";
import { Emitter, Message } from "vscode-languageserver";
import { bootLanguageServer, LanguageMessageWriter, LanguageMessageReader, applyCustomMetadata } from "../src";
import { createConfiguration } from "../src/configuration";
import { mergeMetadata, getDefaultMetadata } from "@naninovel/common";
import * as vscode from "vscode-languageserver/browser";

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

test("publish diagnostics on backend routes to send diagnostics", () => {
    Language.publishDiagnostics("foo", []);
    expect(connection.sendDiagnostics).toBeCalledWith({ diagnostics: [], uri: "foo" });
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

test("when applying custom metadata handlers are re-created with merged meta", () => {
    const custom = { variables: ["foo"] } as Metadata.Project;
    const expectedMerged = mergeMetadata(getDefaultMetadata(), custom);
    applyCustomMetadata(custom);
    expect(Language.createHandlers).toBeCalledWith(expectedMerged);
});
