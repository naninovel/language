import { boot as bootDotNet, terminate as terminateDotNet, Language, Metadata } from "backend";
import { Emitter, Message } from "vscode-languageserver";
import { bootLanguageServer, LanguageMessageWriter, LanguageMessageReader, applyCustomMetadata } from "../src";
import { createConfiguration } from "../src/configuration";
import { mergeMetadata, getDefaultMetadata } from "@naninovel/common";

class MockMessage implements Message {
    constructor(public jsonrpc: string) {}
}

const reader = new Emitter<Message>();
const writer = new Emitter<Message>();

beforeAll(bootDotNet);
afterAll(terminateDotNet);

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
    await new LanguageMessageWriter(writer).write(new MockMessage("foo"));
    expect((await promise).jsonrpc).toEqual("foo");
});

test("can create configuration", () => {
    expect(createConfiguration()).not.toBeNull();
});

test("when applying custom metadata handlers are re-created with merged meta", () => {
    Language.CreateHandlers = jest.fn();
    const custom = { variables: ["foo"] } as Metadata.Project;
    const expectedMerged = mergeMetadata(getDefaultMetadata(), custom);
    applyCustomMetadata(custom);
    expect(Language.CreateHandlers).toBeCalledWith(expectedMerged);
});
