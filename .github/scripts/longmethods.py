import os
import re


def find_matching_bracket(content, start_index):
    bracket_count = 1
    i = start_index + 1
    while i < len(content) and bracket_count > 0:
        if content[i] == "{":
            bracket_count += 1
        elif content[i] == "}":
            bracket_count -= 1
        i += 1
    return i


def process_file(file_path, line_threshold):
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            file_content = f.read()
        process_file_content(file_path, file_content, line_threshold)
    except Exception as e:
        print(f"Error processing file: {file_path}, Error: {e}")


def process_file_content(file_path, file_content, line_threshold):
    class_record_struct_regex = r"class\s+(\w+)|record\s+(\w+)|struct\s+(\w+)"
    method_regex = (
        r"(public|protected|private|internal|static)(\s+\w+)?\s+[\w<>,\s]+\s+(\w+)\s*\("
    )
    classes = re.finditer(class_record_struct_regex, file_content)
    for class_match in classes:
        class_name = (
            class_match.group(1) or class_match.group(2) or class_match.group(3)
        )
        process_class(file_path, file_content, class_name, method_regex, line_threshold)


def process_class(file_path, file_content, class_name, method_regex, line_threshold):
    methods = re.finditer(method_regex, file_content)
    for method_match in methods:
        if any(x in method_match.group(0).lower() for x in ["class", "struct", "record"]):
            continue
        process_method(
            file_path, file_content, class_name, method_match, line_threshold
        )


def process_method(file_path, file_content, class_name, method_match, line_threshold):
    method_name = method_match.group(3)
    method_start_index = method_match.start()
    method_end_index = file_content.find("{", method_start_index)
    if method_end_index > 0:
        method_body_end = find_matching_bracket(file_content, method_end_index)
        method_body = file_content[method_end_index:method_body_end]
        line_count = len(method_body.split("\n"))
        if line_count > line_threshold:
            print(
                f"File: {os.path.basename(file_path)}\n"
                f"Class: {class_name}\n"
                f"Method: {method_name}\n"
                f"Line Count: {line_count}"
            )


def main(parent_directory="./src", line_threshold=21):
    for root, dirs, files in os.walk(parent_directory):
        if "Migrations" in os.path.basename(root):
            continue
        for file in files:
            if file.endswith(".cs"):
                file_path = os.path.join(root, file)
                process_file(file_path, line_threshold)


if __name__ == "__main__":
    main()
