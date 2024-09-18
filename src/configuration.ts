import { TokenHandler } from "backend";
import * as ls from "vscode-languageserver";

const filePattern: ls.FileOperationRegistrationOptions = { filters: [{ pattern: { glob: "**/*.nani" } }] };

export function createConfiguration(): ls.InitializeResult {
    return {
        serverInfo: { name: "Naninovel Language Server" },
        capabilities: createCapabilities()
    };
}

function createCapabilities(): ls.ServerCapabilities {
    return {
        textDocumentSync: ls.TextDocumentSyncKind.Incremental,
        workspace: { fileOperations: { didRename: filePattern, didDelete: filePattern } },
        completionProvider: { triggerCharacters: ["@", ":", "[", " ", ".", ",", "{"] },
        semanticTokensProvider: { legend: TokenHandler.getTokenLegend(), full: { delta: false }, range: true },
        documentSymbolProvider: {},
        foldingRangeProvider: {},
        hoverProvider: {},
        definitionProvider: {},
        renameProvider: { prepareProvider: true }
    };
}
