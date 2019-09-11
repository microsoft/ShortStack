/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

 import * as vscode from 'vscode';
import * as fs from 'fs';

/**
 * Calculates the server path based on the extension path and whether we are in debug (active development) mode.
 */
export function calculateServerPath(extensionContext: vscode.ExtensionContext) : string {
    let serverPath: string;
    /**
     * In debug mode, we want to launch the ShortStack server from where it is built from Visual Studio
     */
    if (extensionStartedInDebugMode()) {
        // First, let's see if we build the x64 CPU version for more memory space
        serverPath = extensionContext.asAbsolutePath("../ShortStackServer/bin/x64/Debug/ShortStackServer.exe");
        if (!fs.existsSync(serverPath)) {
            // Let's try any CPU
            serverPath = extensionContext.asAbsolutePath("../ShortStackServer/bin/Debug/ShortStackServer.exe");
        }
    } else {
        // In release mode, the short-stack server sits right next to the extension
        serverPath  = extensionContext.asAbsolutePath("./out/ShortStackServer.exe");
    }

    return serverPath;
}

/**
 * A helper for detecting whether the extension was started in debug mode.
 * Borrowed from: https://github.com/Microsoft/vscode-languageserver-node/blob/db0f0f8c06b89923f96a8a5aebc8a4b5bb3018ad/client/src/main.ts#L217
 */
function extensionStartedInDebugMode(): boolean {
    let args: string[] = (process as any).execArgv;
    if (args) {
        return args.some((arg) => /^--debug=?/.test(arg) || /^--debug-brk=?/.test(arg) || /^--inspect=?/.test(arg) || /^--inspect-brk=?/.test(arg));
    }
    
    return false;
}