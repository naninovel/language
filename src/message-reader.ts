import { AbstractMessageReader, Emitter, Disposable, MessageReader, Message } from "vscode-languageserver";

export class LanguageMessageReader extends AbstractMessageReader implements MessageReader {
    constructor(private emitter: Emitter<Message>) { super(); }

    listen(callback: (value: Message) => void): Disposable {
        return this.emitter.event(callback);
    }
}
