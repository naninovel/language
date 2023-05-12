import { Language, Metadata } from "backend";
import { getDefaultMetadata, mergeMetadata } from "@naninovel/common";
import { LanguageMessageReader } from "./message-reader";
import { LanguageMessageWriter } from "./message-writer";
import { Message, Connection, Emitter } from "vscode-languageserver";
import { createConnection } from "vscode-languageserver/browser";
import { createConfiguration } from "./configuration";

export function bootLanguageServer(reader: Emitter<Message>, writer: Emitter<Message>) {
    Language.createHandlers(getDefaultMetadata());
    startServer(reader, writer);
}

export function applyCustomMetadata(customMetadata: Metadata.Project) {
    const mergedMeta = mergeMetadata(getDefaultMetadata(), customMetadata);
    Language.createHandlers(mergedMeta);
}

export function loadScriptDocument(uri: string, text: string) {
    Language.openDocument(uri, text);
}

function startServer(reader: Emitter<Message>, writer: Emitter<Message>) {
    const messageReader = new LanguageMessageReader(reader);
    const messageWriter = new LanguageMessageWriter(writer);
    const connection = createConnection(messageReader, messageWriter);
    connection.onInitialize(createConfiguration);
    attachHandlers(connection);
    connection.listen();
}

function attachHandlers(connection: Connection) {
    Language.publishDiagnostics = (uri, diags) => connection.sendDiagnostics({ uri: uri, diagnostics: diags as never });
    connection.onDidOpenTextDocument(p => Language.openDocument(p.textDocument.uri, p.textDocument.text));
    connection.onDidChangeTextDocument(p => Language.changeDocument(p.textDocument.uri, p.contentChanges as never));
    connection.onCompletion(p => Language.complete(p.textDocument.uri, p.position) as never);
    connection.onDocumentSymbol(p => Language.getSymbols(p.textDocument.uri) as never);
    connection.onRequest("textDocument/semanticTokens/full", p => Language.getAllTokens(p.textDocument.uri));
    connection.onRequest("textDocument/semanticTokens/range", p => Language.getTokens(p.textDocument.uri, p.range));
    connection.onHover(p => Language.hover(p.textDocument.uri, p.position) as never);
    connection.onFoldingRanges(p => Language.getFoldingRanges(p.textDocument.uri));
    connection.onDefinition(p => Language.gotoDefinition(p.textDocument.uri, p.position));
}
