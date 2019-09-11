/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

'use strict';

import * as vscode from 'vscode';
import { Server } from './shortStackServer/server';
import { ActiveStackLevelsModel } from './ActiveStackLevelsModel';
import { ActiveStackModel } from './ActiveStackModel';
import { BranchQuickPickItem } from './BranchQuickPickItem';

/**
 * The tree-view item data provider for the "active stacks".
 */
export class ActiveStacksTreeDataProvider implements vscode.TreeDataProvider<ActiveStackModel | ActiveStackLevelsModel>, vscode.Disposable {
    shortStackServer : Server;
    getStackInfoCancellationSource : (vscode.CancellationTokenSource | undefined) = undefined;
    modelChildren : ActiveStackModel[]  = [];
    disposables : vscode.Disposable[] = [];
    extensionContext: vscode.ExtensionContext;
    selectedStack: (ActiveStackModel | undefined);

    private _onDidChangeTreeData: vscode.EventEmitter<ActiveStackModel | ActiveStackLevelsModel> = new vscode.EventEmitter<ActiveStackModel | ActiveStackLevelsModel>();
    readonly onDidChangeTreeData?: vscode.Event<ActiveStackModel| ActiveStackLevelsModel> = this._onDidChangeTreeData.event;

    constructor(shortStackServer : Server, extensionContext: vscode.ExtensionContext) {
        this.shortStackServer = shortStackServer;
        this.extensionContext = extensionContext;
        this.selectedStack = undefined;

        /**
         * Register for our commands.
         */
        this.disposables.push(vscode.commands.registerCommand("shortStack.goToStack", (model : ActiveStackModel) => {
            this.shortStackServer.switchToStack(model.stackInformation);
        }), this);

        this.disposables.push(vscode.commands.registerCommand("shortStack.normalize", (model : ActiveStackModel) => {
            this.shortStackServer.normalizeStack(model.stackInformation);
        }), this);

        this.disposables.push(vscode.commands.registerCommand("shortStack.normalizeWithOrigin", (model : ActiveStackModel) => {
            this.shortStackServer.normalizeStack(model.stackInformation, /*withOrigin*/ true);
        }), this);

        this.disposables.push(vscode.commands.registerCommand("shortStack.newStack", (model : ActiveStackModel) => this.createNewStack()), this);

        /**
         * Register for our commands.
         */
        this.disposables.push(vscode.commands.registerCommand("shortStack.goToLevel", (model : ActiveStackLevelsModel) => {
            if (this.selectedStack) {
                this.shortStackServer.goToLevel(this.selectedStack.stackInformation, model.stackLevel);
            }
        }), this);

        this.disposables.push(vscode.commands.registerCommand("shortStack.goToPullRequest", (model : ActiveStackLevelsModel) => {
            if (this.selectedStack) {
                this.shortStackServer.goToPullRequest(this.selectedStack.stackInformation, model.stackLevel);
            }
        }), this);

        this.disposables.push(vscode.commands.registerCommand("shortStack.createNextStackLevel", (model : ActiveStackLevelsModel) => {
            if (this.selectedStack) {
                this.shortStackServer.goToLevel(this.selectedStack.stackInformation, model.stackLevel);
            }
        }), this);

        this.disposables.push(vscode.commands.registerCommand("shortStack.pushStackLevel", (model : ActiveStackLevelsModel) => {
            if (this.selectedStack) {
                this.shortStackServer.pushStackLevel(this.selectedStack.stackInformation, model.stackLevel);
            }
        }), this);

        const treeProvider : vscode.TreeView<ActiveStackModel | ActiveStackLevelsModel> = vscode.window.createTreeView("shortStack-activeStacks", { treeDataProvider: this });

        treeProvider.onDidChangeSelection((selection) => {
            this.selectedStack =  (selection && selection instanceof ActiveStackModel) ? selection : undefined;
        }, this);
    }

    private createNewStack() : void {
        vscode.window.withProgress({
            location: vscode.ProgressLocation.Window,
            title: "Creating new stack"
        }, async (progress) => {
            if (vscode.window.activeTextEditor && vscode.window.activeTextEditor.document) {
                const fileName : string = vscode.window.activeTextEditor.document.fileName;
                if (fileName) {
                    progress.report( { message: "Retrieving remote branches" });
                    this.shortStackServer.getOriginBranches(vscode.window.activeTextEditor.document.fileName).then((branches) => {
                        if (branches && branches.length !== 0) {
                            vscode.window.showInputBox( {
                                prompt: "Please enter a new stack name:"
                            }).then((stackName => {
                                if (stackName) {
                                    const branchPickItems : BranchQuickPickItem[] = branches.map((branch) => new BranchQuickPickItem(branch));
                                    vscode.window.showQuickPick(branchPickItems, {
                                        canPickMany: false,
                                        placeHolder: "Select origin branch"
                                    }).then((pickedBranch) => {
                                        if (pickedBranch) {
                                            this.shortStackServer.createNewStack(fileName, stackName, pickedBranch.branch.friendlyName);
                                        }
                                    });
                                }
                            }));
                        }
                    });    
                }
            }
        });
    }

    dispose() : void {
        for (const disposable of this.disposables) {
            disposable.dispose();
        }
    }

    /**
     * Refreshes the stacks given the file path opened in the editor.
     * @param editorPath The path for the file that was opened in the editor
     */
    public getStacksForPath(editorPath: string) : void {
        /**
         * Clear out the old tree view items.
         */
        this.modelChildren = [];
        this._onDidChangeTreeData.fire();

        if (editorPath) {
            /**
             * Cancel previous request, if we have one.
             */
            if (this.getStackInfoCancellationSource) {
                this.getStackInfoCancellationSource.cancel();
                this.getStackInfoCancellationSource.dispose();
                this.getStackInfoCancellationSource = new vscode.CancellationTokenSource();
            } else {
                this.getStackInfoCancellationSource = new vscode.CancellationTokenSource();
            }

            const cancelToken : vscode.CancellationToken = this.getStackInfoCancellationSource.token;

            /**
             * Call the short-stack server to get the information about the current stacks.
             */
            this.shortStackServer.getStacksInformation(editorPath, cancelToken).then((stackInfos) =>  {

                /**
                 * When this finishes, and the operation hasn't been cancelled, then go ahead and create
                 * a new set of children.
                 */
                if (!cancelToken.isCancellationRequested) {
                    const newModelChildren : ActiveStackModel[] = [];

                    for (const stackInfo of stackInfos) {
                        newModelChildren.push(new ActiveStackModel(stackInfo, this.extensionContext));
                    }

                    this.modelChildren = newModelChildren;

                    /**
                     * Fire the change notification for our "root" (which is undefined)
                     * to pick up the new children.
                     */
                    this._onDidChangeTreeData.fire();
                }
            });

        }
    }
    
    getTreeItem(element: ActiveStackModel | ActiveStackLevelsModel): Thenable<ActiveStackModel | ActiveStackLevelsModel> {
        return new Promise((resolve, reject) => {

            /**
             * Our tree items happen to be the same as our model (for now)
             * so just return them.
             */
            resolve(element);
        });
    }

    getChildren(element?: ActiveStackModel | ActiveStackLevelsModel) : vscode.ProviderResult<(ActiveStackModel | ActiveStackLevelsModel)[]> {

        if (!element) {
            return this.modelChildren;
        }
        
        if (element instanceof ActiveStackModel) {
            return element.getChildren();
        }

        return [];
    }
}
