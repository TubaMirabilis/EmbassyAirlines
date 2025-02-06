import os


def filter_directories_and_files(dirnames, filename):
    """Filter out specified directories and file extensions."""
    # Exclude directories
    dirnames[:] = [
        d
        for d in dirnames
        if d not in [".git", ".vs", "bin", "lib", "Migrations", "obj"]
    ]
    return not (
        filename.endswith(".csproj")
        or filename.endswith(".feature")
        or filename.endswith(".feature.cs")
        or filename.endswith(".tf")
        or filename.endswith(".ico")
        or filename.endswith(".json")
        or filename.endswith(".md")
        or filename.endswith(".sql")
        or filename.endswith(".sln")
        or filename.endswith(".dot")
        or filename.endswith(".png")
        or filename == ".editorconfig"
        or filename == "nginx.conf"
        or filename == "docker-compose.yml"
        or filename == "db-entity-migration.sh"
    )


def process_file(file_path, max_length):
    """Process a single file, checking for lines exceeding max_length."""
    with open(file_path, "r", encoding="utf-8", errors="ignore") as file:
        line_number = 0
        for line in file:
            line_number += 1
            if len(line) > max_length:
                extra_chars = len(line) - max_length
                print(f"{file_path}:{line_number}: Line is {extra_chars} characters too long")
                print(f"{file_path}:{line_number}: {line.strip()}")


def scan_files_for_long_lines(directory, max_length=170):
    for dirpath, dirnames, filenames in os.walk(directory):
        # Update the exclusion list for directories if needed
        dirnames[:] = [d for d in dirnames if d not in ["bin", "obj"]]

        for filename in filenames:
            if filter_directories_and_files(dirnames, filename):
                file_path = os.path.join(dirpath, filename)
                if os.path.isfile(file_path):
                    process_file(file_path, max_length)


# Replace '.' with the path of the directory you want to scan
directory_to_scan = "."
scan_files_for_long_lines(directory_to_scan)
