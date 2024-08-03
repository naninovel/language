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

function attachHandlers(c: Connection) {
    cs.DiagnosticPublisher.publishDiagnostics = (uri, diags) => c.sendDiagnostics({ uri: uri, diagnostics: diags as never });
    c.onDidOpenTextDocument(p => upsertDocuments([{ uri: p.textDocument.uri, text: p.textDocument.text }]));
    c.onDidChangeTextDocument(p => cs.DocumentHandler.changeDocument(p.textDocument.uri, p.contentChanges as never));
    c.workspace.onDidRenameFiles(p => cs.DocumentHandler.renameDocuments(p.files));
    c.workspace.onDidDeleteFiles(p => cs.DocumentHandler.deleteDocuments(p.files));
    c.onCompletion(p => cs.CompletionHandler.complete(p.textDocument.uri, p.position) as never);
    c.onDocumentSymbol(p => cs.SymbolHandler.getSymbols(p.textDocument.uri) as never);
    c.onRequest("textDocument/semanticTokens/full", p => cs.TokenHandler.getAllTokens(p.textDocument.uri));
    c.onRequest("textDocument/semanticTokens/range", p => cs.TokenHandler.getTokens(p.textDocument.uri, p.range));
    c.onHover(p => cs.HoverHandler.hover(p.textDocument.uri, p.position) as never);
    c.onFoldingRanges(p => cs.FoldingHandler.getFoldingRanges(p.textDocument.uri));
    c.onDefinition(p => cs.DefinitionHandler.gotoDefinition(p.textDocument.uri, p.position));
}
