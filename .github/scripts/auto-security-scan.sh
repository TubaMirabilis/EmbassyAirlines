#!/bin/bash

# Step 1: Run semgrep and save output in JSON format
semgrep --config auto -o results.json --json

# Step 2: Parse the JSON output and create GitHub issues
cat results.json |
  jq -r '.results[] | \
"\(.check_id)\n\(.path)\n\(.start.line)\n\(.extra.message)"' |
  while read -r check_id; do
    read -r path
    read -r line
    read -r message

    # Create a unique identifier for the issue to check for duplicates
    issue_title="Semgrep Finding: $check_id in $path at line $line"

    # Step 3: Check if an issue with the same title already exists
    existing_issue=$(gh issue list -S "$issue_title" -L 1 | wc -l)

    if [ $existing_issue -eq 0 ]; then
      # Step 4: Create a GitHub issue if no duplicates found
      echo "Creating GitHub issue:"
      echo "Title: $issue_title"
      echo "Body: Path: $path
  Line: $line
  Message: $message"
      read -p "Do you want to proceed? (y/n): " create_issue_confirmation
      if [ "$create_issue_confirmation" = "y" ]; then
        gh issue create --title "$issue_title" --body "Path: $path
  Line: $line
  Message: $message"
      else
        echo "Issue creation cancelled."
      fi
    else
      echo "Issue already exists: $issue_title"
    fi
  done

# Step 5: Check for issues in GitHub that are not reported in the results and close them
echo "Checking for issues to close:"
gh issue list --state open --label "auto-security-scan" --json title,number |
  jq -r '.[] | select(.title | startswith("Semgrep Finding:")) | .number' |
  while read -r issue_number; do
    existing_result=$(cat results.json | jq -r '.results[].check_id' | grep -c "$issue_number")

    if [ $existing_result -eq 0 ]; then
      echo "Closing GitHub issue: $issue_number"
      read -p "Do you want to proceed? (y/n): " close_issue_confirmation
      if [ "$close_issue_confirmation" = "y" ]; then
        gh issue close $issue_number
      else
        echo "Issue closure cancelled."
      fi
    fi
  done
