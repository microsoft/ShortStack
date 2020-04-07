import { ShortStackOptions } from "../ShortStackOptions";

//------------------------------------------------------------------------------
// Start a new stack or extend the current stack 1 level
//------------------------------------------------------------------------------
export function newStack(options: ShortStackOptions)
{
    console.log("NEW STACK");
    // if(warn_on_dangling_work)
    // {
    //     return       
    // }

    // $number = 0
    // $stackInfo = get_stack_info $name
    // $origin = $desiredOrigin

    // # if no name, then figure it out from the current branch name
    // if($stackInfo.IsStacked -ne $true)
    // {
    //     Write-host "Current branch is not a stacked branch.  To start a stack:  ss new (name) (origin branch to track)"
    //     return
    // }
    
    // # if this is an active stack, then override the desired origin with the last branch
    // if($stackInfo.Origin -ne $null)
    // {
    //     $lastBranch = make_stackbranch_name $stackInfo.Name $stackInfo.LastBranchNumber
    //     if($lastBranch -ne $stackInfo.Branch)
    //     {
    //         $null = git checkout $lastBranch
    //         $stackInfo = get_stack_info
    //     }
    //     $origin = $stackInfo.Branch
    // }

    // $name = $stackInfo.Name
    // $number = $stackInfo.LastBranchNumber + 1      
    
    // # If we still don't have an origin, default to master
    // if($origin -eq $null)
    // {
    //     $origin = "master"
    // }
    
    // # Make sure we are tracking something from the server
    // if($origin.StartsWith("origin/") -eq $false)
    // {
    //     $origin = "origin/$origin"
    // }
    
    // $newBranch = make_stackbranch_name $name $number 

    // # for new stacks, we need to create a "zero" branch to isolate the
    // # stack from the tracked branch.  This is because the tracked 
    // # branch usually has policies that prevent us from automatically
    // # completing and commiting pull requests when we are finished.  
    // if($number -eq 0)
    // {
    //     write-host -ForegroundColor Gray "Creating branch 'zero' branch to track $origin ..."
    //     git branch $newBranch --track $origin *> $null
    //     git checkout $newBranch   *> $null
    //     $pullResult = git_pull $origin
    //     [void](check_for_good_git_result $pullResult)
    //     $pushResult = git_push origin $newBranch
    //     [void](check_for_good_git_result $pushResult)

    //     $origin = "origin/$newbranch"
    //     $number++
    //     $newBranch = make_stackbranch_name $name $number 
    // }
    
    // write-host -ForegroundColor White "Creating branch $newBranch to track $origin ..."
    // git branch $newBranch --track $origin *> $null
    // git checkout $newBranch   *> $null
   
    // write-host "Synchronizing with tracking branch..."
    // $pullResult = git_pull $origin
    // [void](check_for_good_git_result $pullResult)
   

    // #create the upstream branch
    // write-host "Creating remote version of $newBranch"
    // $pushResult = git_push origin $newBranch
    // [void](check_for_good_git_result $pushResult)

    // write-host -ForegroundColor Green "==================================="
    // write-host -ForegroundColor Green "---  Your new branch is ready!  ---"
    // write-host -ForegroundColor Green "==================================="
    // write-host "Next steps:"
    // write-host "    1) Keep to a 'single-thesis' change for this branch"
    // write-host "    2) make as many commits as you want"
    // write-host "    3) When your change is finished:"
    // write-host "       ss push   <== pushes your changes up and creates a pull request."
    // write-host "       ss new    <== creates the next branch for a new change.`n`n"

}