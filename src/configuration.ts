import * as Backend from "backend";
import * as LS from "vscode-languageserver";

const filePattern: LS.FileOperationRegistrationOptions = { filters: [{ pattern: { glob: "**/*.nani" } }] };

export function createConfiguration(): LS.InitializeResult {
    return {
        serverInfo: { name: "Naninovel Language Server" },
        capabilities: createCapabilities()
    };
}

function createCapabilities(): LS.ServerCapabilities {
    return {
        textDocumentSync: LS.TextDocumentSyncKind.Incremental,
        workspace: { fileOperations: { didRename: filePattern, didDelete: filePattern } },
        completionProvider: { triggerCharacters: ["@", ":", "[", " ", ".", ",", "{"] },
        semanticTokensProvider: { legend: Backend.TokenHandler.getTokenLegend(), full: { delta: false }, range: true },
        documentSymbolProvider: {},
        foldingRangeProvider: {},
        hoverProvider: {},
        definitionProvider: {}
    };
}
