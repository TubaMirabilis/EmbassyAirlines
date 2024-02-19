import subprocess

print("Running dotnet tests...")

# Run the command and suppress its output
result = subprocess.run(
    ["dotnet", "test"], stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT
)

# Check the exit code
if result.returncode == 0:
    print("Success! May the stars shine upon your fortunes.")
else:
    print("Alas! The Dark Lord is watching")

# Print the exit code
print(result.returncode)
