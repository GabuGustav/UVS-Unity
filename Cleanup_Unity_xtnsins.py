import os

# =========================
# CONFIG — edit this part
# =========================

# Any file that ENDS WITH one of these will be deleted
EXTENSIONS_TO_REMOVE = {
    ".meta",
    ".asset",
    # add more here if you want
    # ".tmp",
    # ".bak",
}

# =========================
# SCRIPT LOGIC
# =========================

def should_delete(filename: str) -> bool:
    filename = filename.lower()
    return any(filename.endswith(ext) for ext in EXTENSIONS_TO_REMOVE)


def clean_directory(root_path: str):
    for root, _, files in os.walk(root_path):
        for file in files:
            if should_delete(file):
                full_path = os.path.join(root, file)
                try:
                    os.remove(full_path)
                    print(f"Deleted: {full_path}")
                except Exception as e:
                    print(f"Failed to delete {full_path}: {e}")


if __name__ == "__main__":
    ROOT_DIR = os.path.dirname(os.path.abspath(__file__))
    clean_directory(ROOT_DIR)
