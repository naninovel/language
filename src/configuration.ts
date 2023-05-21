﻿import * as Backend from "backend";
import { ServerCapabilities, TextDocumentSyncKind, InitializeResult } from "vscode-languageserver";

export function createConfiguration(): InitializeResult {
    return {
        serverInfo: { name: "Naninovel Language Server" },
        capabilities: createCapabilities()
    };
}

function createCapabilities(): ServerCapabilities {
    return {
        textDocumentSync: TextDocumentSyncKind.Incremental,
        completionProvider: { triggerCharacters: ["@", ":", "[", " ", ".", ",", "{"] },
        semanticTokensProvider: { legend: Backend.TokenHandler.getTokenLegend(), full: { delta: false }, range: true },
        documentSymbolProvider: {},
        foldingRangeProvider: {},
        hoverProvider: {},
        definitionProvider: {}
    };
}
