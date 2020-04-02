#!/usr/bin/env node
// THis file allows us to make this concole app behave like a command-line binary
// Use  'npm install -g [path to project]' in install
require('ts-node').register({project: require.resolve('../tsconfig.json')});
require ("../src/index.ts")
