import { Language, Metadata } from "backend";
import { getDefaultMetadata, mergeMetadata } from "@naninovel/common";
import { LanguageMessageReader } from "./message-reader";
import { LanguageMessageWriter } from "./message-writer";
import { Message, Connection, Emitter } from "vscode-languageserver";
import { createConnection } from "vscode-languageserver/browser";
import { createConfiguration } from "./configuration";

export function bootLanguageServer(reader: Emitter<Message>, writer: Emitter<Message>) {
    Language.CreateHandlers(getDefaultMetadata());
    startServer(reader, writer);
}

export function applyCustomMetadata(customMetadata: Metadata.Project) {
    const mergedMeta = mergeMetadata(getDefaultMetadata(), customMetadata);
    Language.CreateHandlers(mergedMeta);
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
    Language.PublishDiagnostics = (uri, diags) => connection.sendDiagnostics({ uri: uri, diagnostics: diags as any });
    connection.onDidOpenTextDocument(p => Language.OpenDocument(p.textDocument.uri, p.textDocument.text));
    connection.onDidCloseTextDocument(p => Language.CloseDocument(p.textDocument.uri));
    connection.onDidChangeTextDocument(p => Language.ChangeDocument(p.textDocument.uri, p.contentChanges as any));
    connection.onCompletion(p => Language.Complete(p.textDocument.uri, p.position) as any);
    connection.onDocumentSymbol(p => Language.GetSymbols(p.textDocument.uri) as any);
    connection.onRequest("textDocument/semanticTokens/full", p => Language.GetAllTokens(p.textDocument.uri));
    connection.onRequest("textDocument/semanticTokens/range", p => Language.GetTokens(p.textDocument.uri, p.range));
    connection.onHover(p => Language.Hover(p.textDocument.uri, p.position) as any);
    connection.onFoldingRanges(p => Language.GetFoldingRanges(p.textDocument.uri));
}
