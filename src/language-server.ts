import * as cs from "backend";
import { getDefaultMetadata, mergeMetadata } from "@naninovel/common";
import { createConnection } from "vscode-languageserver/browser";
import { Message, Connection, Emitter } from "vscode-languageserver";
import { LanguageMessageReader } from "./message-reader";
import { LanguageMessageWriter } from "./message-writer";
import { createConfiguration } from "./configuration";

export function bootLanguageServer(reader: Emitter<Message>, writer: Emitter<Message>) {
    cs.Language.bootServer();
    cs.MetadataHandler.updateMetadata(getDefaultMetadata());
    establishConnection(reader, writer);
}

export function applyCustomMetadata(customMetadata: cs.Metadata.Project) {
    const mergedMeta = mergeMetadata(getDefaultMetadata(), customMetadata);
    cs.MetadataHandler.updateMetadata(mergedMeta);
}

export function configure(settings: cs.Language.Settings) {
    cs.SettingsHandler.configure(settings);
}

export function upsertDocuments(docs: cs.Language.DocumentInfo[]) {
    cs.DocumentHandler.upsertDocuments(docs);
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
    cs.DiagnosticPublisher.publishDiagnostics = (uri, diags) => connection.sendDiagnostics({ uri: uri, diagnostics: diags as never });
    connection.onDidOpenTextDocument(p => upsertDocuments([{ uri: p.textDocument.uri, text: p.textDocument.text }]));
    connection.onDidChangeTextDocument(p => cs.DocumentHandler.changeDocument(p.textDocument.uri, p.contentChanges as never));
    connection.workspace.onDidRenameFiles(p => cs.DocumentHandler.renameDocuments(p.files));
    connection.workspace.onDidDeleteFiles(p => cs.DocumentHandler.deleteDocuments(p.files));
    connection.onCompletion(p => cs.CompletionHandler.complete(p.textDocument.uri, p.position) as never);
    connection.onDocumentSymbol(p => cs.SymbolHandler.getSymbols(p.textDocument.uri) as never);
    connection.onRequest("textDocument/semanticTokens/full", p => cs.TokenHandler.getAllTokens(p.textDocument.uri));
    connection.onRequest("textDocument/semanticTokens/range", p => cs.TokenHandler.getTokens(p.textDocument.uri, p.range));
    connection.onHover(p => cs.HoverHandler.hover(p.textDocument.uri, p.position) as never);
    connection.onFoldingRanges(p => cs.FoldingHandler.getFoldingRanges(p.textDocument.uri));
    connection.onDefinition(p => cs.DefinitionHandler.gotoDefinition(p.textDocument.uri, p.position));
}
