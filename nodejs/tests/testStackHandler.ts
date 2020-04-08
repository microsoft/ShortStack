import { ShortStackProcessor } from "../src/models/ShortStackProcessor";
import { expect } from "chai";
import { IGitHelper } from "../src/helpers/gitHelper";

class MockGitHelper implements IGitHelper
{
    CurrentBranch = "testBranch"
    RepositoryRootPath = "repoRoot"
    LocalBranches = [];
    HasUncommittedChanges = false;
}

describe('StackHandler.CreateNewStack', function() {
    it('fails when there is dangling work', function() {   
      const mockGit = new MockGitHelper();
      mockGit.HasUncommittedChanges = true;
      const target = new ShortStackProcessor(mockGit);
      expect(() => target.createNewStack("","")).to.throw("There are uncommitted changes.");
    }); 


  }); 

