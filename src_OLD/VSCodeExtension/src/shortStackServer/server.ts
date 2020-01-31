/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

import { createMessageConnection, MessageConnection, MessageReader, MessageWriter, generateRandomPipeName, createClientPipeTransport, RequestType, CancellationToken, NotificationType } from "vscode-jsonrpc";
import { spawn } from "child_process";
import * as vscode from 'vscode';

export class Server {
    /**
     * The outgoing JSON-RPC message types.
     * What this is saying is that we are going to send a message to the Short-Stack JSON-RPC server with the name of "sayHello" which
     * matches message identifier in our .Net server.
     * It will send an object with the fields of type @see JsonRpcTypes.HelloWorldRequestParams and well expect back
     * and object of type @see JsonRpcTypes.HelloWorldResponse
     */
    private helloWorldRequest: RequestType<JsonRpcTypes.HelloWorldRequestParams, JsonRpcTypes.HelloWorldResponse, void, void> = new RequestType('sayHello');
    private stackInfoRequest: RequestType<JsonRpcTypes.StackInfoRequestParams, JsonRpcTypes.StackInfoRequestResponse, void, void> = new RequestType('getStackInfoFromFilePath');
    private switchToStackRequest: RequestType<JsonRpcTypes.NormalizeStackRequestParams, void, void, void> = new RequestType('switchToStack');
    private normalizeStackRequest: RequestType<JsonRpcTypes.NormalizeStackRequestParams, void, void, void> = new RequestType('normalizeStack');
    private goToStackLevelRequest: RequestType<JsonRpcTypes.GoToStackLevelRequestParams, void, void, void> = new RequestType('goToStackLevel');
    private goToPullRequestRequest: RequestType<JsonRpcTypes.GoToPullRequestRequestParams, void, void, void> = new RequestType('goToPullRequest');
    private getOriginBranchesRequest: RequestType<JsonRpcTypes.GetOriginBranchesRequestParams, JsonRpcTypes.GetOriginBranchesResponse, void, void> = new RequestType('getOriginBranches');
    private createNewStackRequest: RequestType<JsonRpcTypes.NewStackRequestParams, void, void, void> = new RequestType('createNewStack');
    private createNewStackLevelRequest: RequestType<JsonRpcTypes.CreateNextStackLevelRequestParams, void, void, void> = new RequestType('createNewStackLevel');
    private pushStackLevelRequest: RequestType<JsonRpcTypes.PushStackLevelRequestParams, void, void, void> = new RequestType('pushStackLevel');

    /**
     * Incoming notifications from Server
     */

    private newStackCreatedNotificationType: NotificationType<string, void> = new NotificationType('newStackCreated');

    /**
     * Holds the JSON-RPC connection to the server.
     */
    private messageConnection: MessageConnection;

    /**
     * Handle the new-stack created notification.
     */
    private _newStackCreated: vscode.EventEmitter<string> = new vscode.EventEmitter<string>();
    readonly newStackCreated: vscode.Event<string> = this._newStackCreated.event;

    private newStackCreatedNotificationHandler(stackName: string) : void {
        this._newStackCreated.fire(stackName);
    }

    /**
     * Constructs the server class.
     * @param messageConnection The JSON-RPC connection.
     */
    private constructor(messageConnection: MessageConnection) {
        this.messageConnection = messageConnection;

        /**
         * Set up notifications.
         */
        this.messageConnection.onNotification(this.newStackCreatedNotificationType, this.newStackCreatedNotificationHandler.bind(this));
    }

    /**
     * Launches and connects to the short-stack server.
     * @param serverPath The path to the Short-stack server executable.
     * @param debugOnStart Set to true if you desire the short-stack server to issue a debugger.launch on startup.
     */
    public static Connect(serverPath: string, debugOnStart: boolean): Thenable<Server> {
        return new Promise((resolve, reject) => {
            // Generates a unique pipe-name (using a GUID) that is guaranteed to not
            // collide with any other local pipe name
            const pipeName: string = generateRandomPipeName();

            // Now, using the vscode-jsonrpc library to create the pipe.
            createClientPipeTransport(pipeName).then(transport => {
                /**
                 * Now, we have a pipe listening, but, no one has connected to it yet, so nothing is going to happen.
                 * So... now we need to spin up our short-stack server and give it our pipe name.
                 */
                const args: string[] = <string[]>[
                    "--pipe",
                    pipeName,
                    debugOnStart ? "--debugOnStart" : undefined,
                ].filter((argument) => argument !== undefined);

                /**
                 * Well, launch it! (and from here on out, all the magic happens through events and messages)
                */
                spawn(serverPath, args, { env: process.env });

                // Next, listen for a connection from the short-stack server which we get through the onConnected event.
                transport.onConnected().then(protocol => {
                    // Now, set up our connection. The reader is the first object in the response
                    // and the writer is the second.
                    const reader: MessageReader = protocol[0];
                    const writer: MessageWriter = protocol[1];

                    const connection: MessageConnection = createMessageConnection(reader, writer);

                    // Finally, tell it to start listening to incoming messages (yes, the pipe is bi-directional)
                    // This is a non-blocking call.
                    connection.listen();

                    resolve(new Server(connection));
                });
            });
        });
    }

    /**
     * Executes the SayHello API in the short-stack server.
     * @param name The name you want to have hello said to.
     */
    public SayHello(name: string) : Thenable<string> {
        return this.messageConnection.sendRequest(this.helloWorldRequest, { yourName: name }).then((response) => {
            return response.helloWorld;
        });
    }

    /**
     * Gets the stack information based on the file path passed in.
     * @param editorPath The file path that the VSCode editor still has open.
     * @param cancellationToken The cancellation token
     * @returns Returns the stack information.
     */
    public getStacksInformation(editorPath: string, cancellationToken?: CancellationToken) : Thenable<JsonRpcTypes.StackInfo[]> {
        return this.messageConnection.sendRequest(this.stackInfoRequest, {editorPath: editorPath}, cancellationToken).then((response) => {
            return response.stackInfo;
        });
    }

    /**
     * Changes the current active stack.
     * @param stackInfo The stack to switch to.
     * @param cancellationToken The cancellation token
     */
    public switchToStack(stackInfo: JsonRpcTypes.StackInfo, cancellationToken?: CancellationToken) : Thenable<void> {
        return this.messageConnection.sendRequest(this.switchToStackRequest, {stackInfo : stackInfo}, cancellationToken);
    }

    /**
     * Normalizes the given stack.
     * @param stackInfo The stack to normalize.
     * @param withOrigin True to normalize the stack with master.
     * @param cancellationToken The cancellation token.
     */
    public normalizeStack(stackInfo: JsonRpcTypes.StackInfo, withOrigin?: boolean, cancellationToken?: CancellationToken) : Thenable<void> {
        return this.messageConnection.sendRequest(this.normalizeStackRequest, {stackInfo : stackInfo, normalizeWithOrigin: withOrigin}, cancellationToken);
    }

    /**
     * Changes the current active stack level.
     * @param stackInfo The stack to which the level to switch to belongs.
     * @param stackLevel The level  to switch to.
     * @param cancellationToken The cancellation token
     */
    public goToLevel(stackInfo: JsonRpcTypes.StackInfo, stackLevel: JsonRpcTypes.StackLevel, cancellationToken?: CancellationToken) : Thenable<void> {
        return this.messageConnection.sendRequest(this.goToStackLevelRequest, {stackInfo : stackInfo, stackLevel: stackLevel}, cancellationToken);
    }

    /**
     * Goes to a pull-request for a stack-level.
     * @param stackInfo The stack to which the level to switch to belongs.
     * @param stackLevel The level to which to view the pull-request for.
     * @param cancellationToken The cancellation token
     */
    public goToPullRequest(stackInfo: JsonRpcTypes.StackInfo, stackLevel: JsonRpcTypes.StackLevel, cancellationToken?: CancellationToken) : Thenable<void> {
        return this.messageConnection.sendRequest(this.goToPullRequestRequest, {stackInfo : stackInfo, stackLevel: stackLevel}, cancellationToken);
    }

    /**
     * Gets the origin branches for a Git repository.
     * @param startPath The path to locate the GIT repository and retrieve the remote branches.
     * @param cancellationToken The cancellation token
     */
    public getOriginBranches(startPath: string, cancellationToken?: CancellationToken) : Thenable<JsonRpcTypes.OriginBranchInformation[]> {
        return this.messageConnection.sendRequest(this.getOriginBranchesRequest, {startPath: startPath}, cancellationToken).then((response) => {
            return response.branches;
        });
    }

    /**
     * Gets the origin branches for a Git repository.
     * @param stackInfo The stack to create a new level in.
     * @param cancellationToken The cancellation token
     */
    public createNewStackLevel(stackInfo: JsonRpcTypes.StackInfo, cancellationToken?: CancellationToken) : Thenable<void> {
        return this.sendRequest(this.createNewStackLevelRequest, {
            stackInfo: stackInfo
        });
    }

    /**
     * Gets the origin branches for a Git repository.
     * @param stackInfo The stack that contains the level.
     * @param stackLevel The level to push.
     * @param cancellationToken The cancellation token
     */
    public pushStackLevel(stackInfo: JsonRpcTypes.StackInfo, stackLevel: JsonRpcTypes.StackLevel, cancellationToken?: CancellationToken) : Thenable<void> {
        return this.sendRequest(this.pushStackLevelRequest, {
            stackInfo: stackInfo,
            stackLevel: stackLevel,
        });
    }

    /**
     * Gets the origin branches for a Git repository.
     * @param startPath The path to locate the GIT repository and retrieve the remote branches.
     * @param originBranch The branch to originate the new stack from.
     * @param cancellationToken The cancellation token
     */
    public createNewStack(startPath: string, stackName: string, originBranch: string, cancellationToken?: CancellationToken) : Thenable<void> {
        return this.sendRequest(this.createNewStackRequest, {
            startPath: startPath,
            stackName: stackName,
            desiredOriginBranch: originBranch
        });
    }

    private sendRequest<P, R, E, RO>(type: RequestType<P, R, E, RO>, params: P, token?: CancellationToken): Thenable<R> {
        return new Promise((resolve, reject) => {
            this.messageConnection.sendRequest(
                type, params, token).then((response) => {
                    resolve();
                }, (reason) => {
                    vscode.window.showErrorMessage(reason.message);
                    reject();
                });
            }
        );
    }
}