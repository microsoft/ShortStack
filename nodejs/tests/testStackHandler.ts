import { StackHandler } from "../src/models/stackHandler";
import { expect } from "chai";
import { IGitHelper } from "../src/helpers/gitHelper";

class MockGitHelper implements IGitHelper
{
    HasUncommittedChanges = false;
}

describe('StackHandler.CreateNewStack', function() {
    const mockGit = new MockGitHelper();
    mockGit.HasUncommittedChanges = true;
    const target = new StackHandler(mockGit);
    it('fails when there is dangling work', function() {   
      expect(() => target.createNewStack("","")).to.throw("There are uncommitted changes.");
    }); 
  });

