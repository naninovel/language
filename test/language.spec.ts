import { boot, terminate } from "backend";
import { Emitter, Message } from "vscode-languageserver";
import { bootLanguageServer, LanguageMessageWriter, LanguageMessageReader } from "../src";

class MockMessage implements Message {
    constructor(public jsonrpc: string) {}
}

const reader = new Emitter<Message>();
const writer = new Emitter<Message>();

describe("language server", () => {
    beforeAll(boot);
    it("can boot", () => {
        expect(() => bootLanguageServer(reader, writer)).not.toThrow();
    });
    afterAll(terminate);
});

describe("in-process transport", () => {
    it("can read messages", async () => {
        let resolve, promise = new Promise<Message>(r => resolve = r);
        new LanguageMessageReader(reader).listen(resolve);
        reader.fire(new MockMessage("foo"));
        expect((await promise).jsonrpc).toEqual("foo");
    });
    it("can write messages", async () => {
        let resolve: any, promise = new Promise<Message>(r => resolve = r);
        writer.event(resolve);
        await new LanguageMessageWriter(writer).write(new MockMessage("foo"));
        expect((await promise).jsonrpc).toEqual("foo");
    });
});
