import * as Backend from "backend";
import { getDefaultMetadata, mergeMetadata } from "@naninovel/common";
import { createConnection } from "vscode-languageserver/browser";
import { Message, Connection, Emitter } from "vscode-languageserver";
import { LanguageMessageReader } from "./message-reader";
import { LanguageMessageWriter } from "./message-writer";
import { createConfiguration } from "./configuration";

export function bootLanguageServer(reader: Emitter<Message>, writer: Emitter<Message>) {
    Backend.Language.bootServer();
    Backend.MetadataHandler.updateMetadata(getDefaultMetadata());
    establishConnection(reader, writer);
}

export function applyCustomMetadata(customMetadata: Backend.Metadata.Project) {
    const mergedMeta = mergeMetadata(getDefaultMetadata(), customMetadata);
    Backend.MetadataHandler.updateMetadata(mergedMeta);
}

export function configure(settings: Backend.Language.Settings) {
    Backend.SettingsHandler.configure(settings);
}

export function upsertDocuments(docs: Backend.Language.DocumentInfo[]) {
    Backend.DocumentHandler.upsertDocuments(docs);
}

function establishConnection(reader: Emitter<Message>, writer: Emitter<Message>) {
    const messageReader = new LanguageMessageReader(reader);
    const messageWriter = new LanguageMessageWriter(writer);
    const connection = createConnection(messageReader, messageWriter);
    connection.onInitialize(createConfiguration);
    attachHandlers(connection);
    connection.listen();
}

function attachHandlers(connection: Connection) {
    Backend.DiagnosticPublisher.publishDiagnostics = (uri, diags) => connection.sendDiagnostics({ uri: uri, diagnostics: diags as never });
    connection.onDidOpenTextDocument(p => upsertDocuments([{ uri: p.textDocument.uri, text: p.textDocument.text }]));
    connection.onDidChangeTextDocument(p => Backend.DocumentHandler.changeDocument(p.textDocument.uri, p.contentChanges as never));
    connection.onCompletion(p => Backend.CompletionHandler.complete(p.textDocument.uri, p.position) as never);
    connection.onDocumentSymbol(p => Backend.SymbolHandler.getSymbols(p.textDocument.uri) as never);
    connection.onRequest("textDocument/semanticTokens/full", p => Backend.TokenHandler.getAllTokens(p.textDocument.uri));
    connection.onRequest("textDocument/semanticTokens/range", p => Backend.TokenHandler.getTokens(p.textDocument.uri, p.range));
    connection.onHover(p => Backend.HoverHandler.hover(p.textDocument.uri, p.position) as never);
    connection.onFoldingRanges(p => Backend.FoldingHandler.getFoldingRanges(p.textDocument.uri));
    connection.onDefinition(p => Backend.DefinitionHandler.gotoDefinition(p.textDocument.uri, p.position));
}
