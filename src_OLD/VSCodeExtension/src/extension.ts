/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

'use strict';

import * as vscode from 'vscode';
import { calculateServerPath } from './helpers';
import { Server } from './shortStackServer/server';
import { ActiveStacksTreeDataProvider  } from './ActiveStacksTreeDataProvider';

/**
 * The entry point to this extension. Called by VS Code upon one of the activation events defined
 * in package.json.
 * @param context The ExtensionContext object passed in my VS Code.
 */
export function activate(vsCodeExtensionContext: vscode.ExtensionContext): Thenable<void> {
    /**
     * If the user has said debug the short-stack server, then add the --debugOnStart command line option
     * so we can attach a debugger before we start communicating with it.
     */
    const debugOnStart: boolean = (vscode.workspace.getConfiguration('shortstack').get<boolean>("debug-server")) || false;
    /**
     * Calculate the server path given our extensions location.
     */
    const serverPath: string = calculateServerPath(vsCodeExtensionContext);

    return Server.Connect(serverPath, debugOnStart).then((connectedServer) => {
        connectedServer.newStackCreated((newStack : string) => {
            if (vscode.window.activeTextEditor && vscode.window.activeTextEditor.document && vscode.window.activeTextEditor.document.fileName) {
                activeStacksProvider.getStacksForPath(vscode.window.activeTextEditor.document.fileName);
            }    
        });

        /**
         * Register the tree view providers that will ultimately provide a view to the user.
         */
        const activeStacksProvider : ActiveStacksTreeDataProvider = new ActiveStacksTreeDataProvider(connectedServer, vsCodeExtensionContext);

        /**
         * When a text document is opened, lets ask the ShortStack server for the state of the world.
         */
        vscode.workspace.onDidOpenTextDocument((document : vscode.TextDocument) => {
            activeStacksProvider.getStacksForPath(document.fileName);
        });

        /**
         * Go ahead and get the stacks for the open document (if any).
         */
        if (vscode.window.activeTextEditor && vscode.window.activeTextEditor.document && vscode.window.activeTextEditor.document.fileName) {
            activeStacksProvider.getStacksForPath(vscode.window.activeTextEditor.document.fileName);
        }
    });
}
