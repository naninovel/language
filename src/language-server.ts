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

function startServer(reader: Emitter<Message>, writer: Emitter<Message>) {
    const messageReader = new LanguageMessageReader(reader);
    const messageWriter = new LanguageMessageWriter(writer);
    const connection = createConnection(messageReader, messageWriter);
    connection.onInitialize(createConfiguration);
    attachHandlers(connection);
    connection.listen();
}

function attachHandlers(connection: Connection) {
    Language.publishDiagnostics = (uri, diags) => connection.sendDiagnostics({ uri: uri, diagnostics: diags as any });
    connection.onDidOpenTextDocument(p => Language.openDocument(p.textDocument.uri, p.textDocument.text));
    connection.onDidCloseTextDocument(p => Language.closeDocument(p.textDocument.uri));
    connection.onDidChangeTextDocument(p => Language.changeDocument(p.textDocument.uri, p.contentChanges as any));
    connection.onCompletion(p => Language.complete(p.textDocument.uri, p.position) as any);
    connection.onDocumentSymbol(p => Language.getSymbols(p.textDocument.uri) as any);
    connection.onRequest("textDocument/semanticTokens/full", p => Language.getAllTokens(p.textDocument.uri));
    connection.onRequest("textDocument/semanticTokens/range", p => Language.getTokens(p.textDocument.uri, p.range));
    connection.onHover(p => Language.hover(p.textDocument.uri, p.position) as any);
    connection.onFoldingRanges(p => Language.getFoldingRanges(p.textDocument.uri));
}
