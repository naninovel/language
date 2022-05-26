import { AbstractMessageWriter, Emitter, MessageWriter, Message } from "vscode-languageserver";

export class LanguageMessageWriter extends AbstractMessageWriter implements MessageWriter {
    constructor(private emitter: Emitter<Message>) { super(); }

    write(msg: Message): Promise<void> {
        this.emitter.fire(msg);
        return Promise.resolve();
    }

    end(): void { }
}
