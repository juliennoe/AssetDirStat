# **Asset Dir Stat**

**A Unity Editor tool to analyze and visualize the disk usage of assets grouped by type and size.**

---

## **📊 Features**

- 📁 **Global Scan**: Analyzes all files in the `Assets/` directory.
- 🎨 **Color Blocks by Type**: Displays asset types as colored buttons (e.g., `.png`, `.mat`, `.cs`).
- 📦 **Size Breakdown**: Shows total size and number of files per extension.
- 🔍 **Clickable Asset List**: Click a file type to list all matching assets and locate them in the Project window.
- 🚫 **Play Mode Safe**: Automatically disables itself during Play Mode to prevent scanning conflicts.
- 🔄 **Refresh & Clear**: Manually scan again or reset the data via buttons.

---

## **📦 Installation**

1. Copy the script into a folder under `Assets/Editor/AssetDirStat.cs`.
2. Or install it via Unity Package Manager using this repository as a Git URL.

---

## **🧠 Usage**

- Open the tool from `Tools > Analysis > Asset Dir Stat`.
- Click **Scan** to analyze the project.
- Select a file type on the left panel to view related assets.
- Click an asset name to ping it in the Project.

---
```json
"com.juliennoe.assetdiskstat": "https://github.com/juliennoe/assetdiskstat.git"
```

## **🧑‍💻 Author**

**Julien Noé** — [GitHub](https://github.com/juliennoe)
