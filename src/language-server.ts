import * as cs from "backend";
import { getDefaultMetadata, mergeMetadata } from "@naninovel/common";
import { createConnection } from "vscode-languageserver/browser";
import { Message, Connection, WorkspaceEdit, FileEvent, Emitter, FileChangeType } from "vscode-languageserver";
import { LanguageMessageReader } from "./message-reader";
import { LanguageMessageWriter } from "./message-writer";
import { createConfiguration } from "./configuration";
import { TextDocumentEdit } from "vscode-languageserver-types";

export function bootLanguageServer(reader: Emitter<Message>, writer: Emitter<Message>) {
    cs.Language.bootServer();
    cs.MetadataUpdater.updateMetadata(getDefaultMetadata());
    establishConnection(reader, writer);
}

export function applyCustomMetadata(customMetadata: cs.Metadata.Project) {
    const mergedMeta = mergeMetadata(getDefaultMetadata(), customMetadata);
    cs.MetadataUpdater.updateMetadata(mergedMeta);
}

export function configure(settings: cs.Language.Settings) {
    cs.Configurator.configure(settings);
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
    cs.EditPublisher.publishEdit = (label, edit) => c.workspace.applyEdit({ label, edit: asEdit(edit)! });
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
    c.onRenameRequest(p => asEdit(cs.RenameHandler.rename(p.textDocument.uri, p.position, p.newName)));
    c.onPrepareRename(p => cs.RenameHandler.prepareRename(p.textDocument.uri, p.position));
    c.onDocumentFormatting(p => cs.FormattingHandler.format(p.textDocument.uri));
    c.onDidChangeWatchedFiles(p => handleFileChanges(p.changes));
}

function handleFileChanges(events: FileEvent[]) {
    if (events.length !== 2) return;
    const created = events.find(e => isFolder(e.uri) && e.type === FileChangeType.Created);
    const deleted = events.find(e => isFolder(e.uri) && e.type === FileChangeType.Deleted);
    if (!created || !deleted) return;
    cs.DocumentHandler.renameDocuments([{ newUri: created.uri, oldUri: deleted.uri }]);
}

function isFolder(uri: string) {
    return !uri.includes(".");
}

function asEdit(edit: cs.Language.WorkspaceEdit | null): WorkspaceEdit | null {
    if (!edit) return null;
    return {
        documentChanges: edit.documentChanges.map(c => ({
            textDocument: { uri: c.textDocument, version: null },
            edits: c.edits
        } satisfies TextDocumentEdit)),
        changeAnnotations: edit.changeAnnotations as never
    };
}
