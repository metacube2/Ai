#!/usr/bin/env python3
"""
DCTP GUI - Delta Code Transfer Protocol graphical user interface.

A CustomTkinter-based GUI for managing AI-generated code transfers
using delta operations for efficient updates.
"""

import os
import sys
from datetime import datetime
from pathlib import Path
from tkinter import filedialog, messagebox
from typing import Optional
import json

import customtkinter as ctk

from dctp_parser import DCTPParser, ParseResult, Operation, OperationType
from dctp_executor import DCTPExecutor, ExecutionResult, PreviewResult, ResultStatus
from dctp_backup import BackupManager
from dctp_diff import DiffType


class SettingsDialog(ctk.CTkToplevel):
    """Settings dialog window."""

    def __init__(self, parent, settings: dict):
        super().__init__(parent)

        self.title("Einstellungen")
        self.geometry("500x400")
        self.resizable(False, False)

        self.settings = settings.copy()
        self.result = None

        # Make modal
        self.transient(parent)
        self.grab_set()

        self._create_widgets()

        # Center on parent
        self.update_idletasks()
        x = parent.winfo_x() + (parent.winfo_width() - self.winfo_width()) // 2
        y = parent.winfo_y() + (parent.winfo_height() - self.winfo_height()) // 2
        self.geometry(f"+{x}+{y}")

    def _create_widgets(self):
        # Main frame
        main_frame = ctk.CTkFrame(self)
        main_frame.pack(fill="both", expand=True, padx=20, pady=20)

        # Project path
        ctk.CTkLabel(main_frame, text="Standard-Projektpfad:").pack(anchor="w", pady=(0, 5))
        path_frame = ctk.CTkFrame(main_frame)
        path_frame.pack(fill="x", pady=(0, 15))

        self.path_entry = ctk.CTkEntry(path_frame, width=350)
        self.path_entry.pack(side="left", fill="x", expand=True, padx=(0, 10))
        self.path_entry.insert(0, self.settings.get("project_path", ""))

        ctk.CTkButton(
            path_frame,
            text="...",
            width=40,
            command=self._browse_path
        ).pack(side="right")

        # Backup directory
        ctk.CTkLabel(main_frame, text="Backup-Verzeichnis:").pack(anchor="w", pady=(0, 5))
        backup_frame = ctk.CTkFrame(main_frame)
        backup_frame.pack(fill="x", pady=(0, 15))

        self.backup_entry = ctk.CTkEntry(backup_frame, width=350)
        self.backup_entry.pack(side="left", fill="x", expand=True, padx=(0, 10))
        self.backup_entry.insert(0, self.settings.get("backup_dir", ".dctp_backups"))

        ctk.CTkButton(
            backup_frame,
            text="...",
            width=40,
            command=self._browse_backup
        ).pack(side="right")

        # Options
        options_frame = ctk.CTkFrame(main_frame)
        options_frame.pack(fill="x", pady=15)

        self.auto_renumber_var = ctk.BooleanVar(
            value=self.settings.get("auto_renumber", True)
        )
        ctk.CTkCheckBox(
            options_frame,
            text="Auto-Renumber nach Operationen",
            variable=self.auto_renumber_var
        ).pack(anchor="w", pady=5)

        self.validate_checksum_var = ctk.BooleanVar(
            value=self.settings.get("validate_checksum", True)
        )
        ctk.CTkCheckBox(
            options_frame,
            text="Checksum-Validierung aktiviert",
            variable=self.validate_checksum_var
        ).pack(anchor="w", pady=5)

        # Theme
        ctk.CTkLabel(main_frame, text="Theme:").pack(anchor="w", pady=(15, 5))
        self.theme_var = ctk.StringVar(value=self.settings.get("theme", "dark"))
        theme_frame = ctk.CTkFrame(main_frame)
        theme_frame.pack(fill="x", pady=(0, 15))

        ctk.CTkRadioButton(
            theme_frame,
            text="Dunkel",
            variable=self.theme_var,
            value="dark"
        ).pack(side="left", padx=(0, 20))

        ctk.CTkRadioButton(
            theme_frame,
            text="Hell",
            variable=self.theme_var,
            value="light"
        ).pack(side="left")

        # Buttons
        button_frame = ctk.CTkFrame(main_frame)
        button_frame.pack(fill="x", pady=(20, 0))

        ctk.CTkButton(
            button_frame,
            text="Abbrechen",
            command=self._cancel
        ).pack(side="right", padx=(10, 0))

        ctk.CTkButton(
            button_frame,
            text="Speichern",
            command=self._save
        ).pack(side="right")

    def _browse_path(self):
        path = filedialog.askdirectory(
            initialdir=self.path_entry.get() or os.path.expanduser("~")
        )
        if path:
            self.path_entry.delete(0, "end")
            self.path_entry.insert(0, path)

    def _browse_backup(self):
        path = filedialog.askdirectory(
            initialdir=os.path.expanduser("~")
        )
        if path:
            self.backup_entry.delete(0, "end")
            self.backup_entry.insert(0, path)

    def _save(self):
        self.result = {
            "project_path": self.path_entry.get(),
            "backup_dir": self.backup_entry.get(),
            "auto_renumber": self.auto_renumber_var.get(),
            "validate_checksum": self.validate_checksum_var.get(),
            "theme": self.theme_var.get()
        }
        self.destroy()

    def _cancel(self):
        self.result = None
        self.destroy()


class DCTPApp(ctk.CTk):
    """Main DCTP application window."""

    SETTINGS_FILE = ".dctp_settings.json"

    def __init__(self):
        super().__init__()

        self.title("DCTP - Delta Code Transfer")
        self.geometry("1200x900")
        self.minsize(800, 600)

        # Initialize components
        self.parser = DCTPParser()
        self.executor: Optional[DCTPExecutor] = None
        self.backup_manager: Optional[BackupManager] = None
        self.current_operations: list[Operation] = []
        self.current_previews: list[PreviewResult] = []

        # Load settings
        self.settings = self._load_settings()
        ctk.set_appearance_mode(self.settings.get("theme", "dark"))

        # Create UI
        self._create_widgets()

        # Initialize project if path is set
        if self.settings.get("project_path"):
            self._init_project(self.settings["project_path"])

        self._log("Bereit")

    def _load_settings(self) -> dict:
        """Load settings from file."""
        settings_path = Path.home() / self.SETTINGS_FILE
        if settings_path.exists():
            try:
                return json.loads(settings_path.read_text())
            except (json.JSONDecodeError, IOError):
                pass
        return {
            "project_path": "",
            "backup_dir": ".dctp_backups",
            "auto_renumber": True,
            "validate_checksum": True,
            "theme": "dark"
        }

    def _save_settings(self):
        """Save settings to file."""
        settings_path = Path.home() / self.SETTINGS_FILE
        settings_path.write_text(json.dumps(self.settings, indent=2))

    def _create_widgets(self):
        """Create all UI widgets."""
        # Top bar - project selection
        top_frame = ctk.CTkFrame(self)
        top_frame.pack(fill="x", padx=10, pady=10)

        ctk.CTkLabel(top_frame, text="Projekt:").pack(side="left", padx=(0, 10))

        self.project_entry = ctk.CTkEntry(top_frame, width=400)
        self.project_entry.pack(side="left", fill="x", expand=True, padx=(0, 10))
        self.project_entry.insert(0, self.settings.get("project_path", ""))

        ctk.CTkButton(
            top_frame,
            text="Waehlen",
            width=100,
            command=self._browse_project
        ).pack(side="left", padx=(0, 10))

        ctk.CTkButton(
            top_frame,
            text="Einstellungen",
            width=100,
            command=self._open_settings
        ).pack(side="left")

        # Main content area with paned layout
        content_frame = ctk.CTkFrame(self)
        content_frame.pack(fill="both", expand=True, padx=10, pady=(0, 10))

        # Left side - Input and preview
        left_frame = ctk.CTkFrame(content_frame)
        left_frame.pack(side="left", fill="both", expand=True, padx=(0, 5))

        # Input area
        input_label_frame = ctk.CTkFrame(left_frame)
        input_label_frame.pack(fill="x", pady=(5, 5), padx=5)
        ctk.CTkLabel(
            input_label_frame,
            text="Input (KI-Output hier einfuegen)",
            font=ctk.CTkFont(weight="bold")
        ).pack(side="left")

        self.input_text = ctk.CTkTextbox(
            left_frame,
            height=250,
            font=ctk.CTkFont(family="Consolas", size=12)
        )
        self.input_text.pack(fill="both", expand=True, padx=5, pady=(0, 10))

        # Buttons
        button_frame = ctk.CTkFrame(left_frame)
        button_frame.pack(fill="x", padx=5, pady=(0, 10))

        self.analyze_btn = ctk.CTkButton(
            button_frame,
            text="Analysieren",
            command=self._analyze,
            width=120
        )
        self.analyze_btn.pack(side="left", padx=(0, 10))

        self.execute_btn = ctk.CTkButton(
            button_frame,
            text="Ausfuehren",
            command=self._execute,
            width=120,
            state="disabled"
        )
        self.execute_btn.pack(side="left", padx=(0, 10))

        self.undo_btn = ctk.CTkButton(
            button_frame,
            text="Undo",
            command=self._undo,
            width=80
        )
        self.undo_btn.pack(side="left", padx=(0, 10))

        ctk.CTkButton(
            button_frame,
            text="Clear",
            command=self._clear,
            width=80
        ).pack(side="left")

        # Preview operations
        preview_label_frame = ctk.CTkFrame(left_frame)
        preview_label_frame.pack(fill="x", pady=(5, 5), padx=5)
        ctk.CTkLabel(
            preview_label_frame,
            text="Vorschau Operationen",
            font=ctk.CTkFont(weight="bold")
        ).pack(side="left")

        self.preview_text = ctk.CTkTextbox(
            left_frame,
            height=150,
            font=ctk.CTkFont(family="Consolas", size=11)
        )
        self.preview_text.pack(fill="both", expand=True, padx=5, pady=(0, 10))
        self.preview_text.configure(state="disabled")

        # Right side - Diff and file tree
        right_frame = ctk.CTkFrame(content_frame)
        right_frame.pack(side="right", fill="both", expand=True, padx=(5, 0))

        # Diff view
        diff_label_frame = ctk.CTkFrame(right_frame)
        diff_label_frame.pack(fill="x", pady=(5, 5), padx=5)
        ctk.CTkLabel(
            diff_label_frame,
            text="Diff-Ansicht",
            font=ctk.CTkFont(weight="bold")
        ).pack(side="left")

        self.diff_text = ctk.CTkTextbox(
            right_frame,
            height=300,
            font=ctk.CTkFont(family="Consolas", size=11)
        )
        self.diff_text.pack(fill="both", expand=True, padx=5, pady=(0, 10))
        self.diff_text.configure(state="disabled")

        # File tree
        tree_label_frame = ctk.CTkFrame(right_frame)
        tree_label_frame.pack(fill="x", pady=(5, 5), padx=5)
        ctk.CTkLabel(
            tree_label_frame,
            text="Projektdateien",
            font=ctk.CTkFont(weight="bold")
        ).pack(side="left")

        ctk.CTkButton(
            tree_label_frame,
            text="Aktualisieren",
            width=80,
            command=self._refresh_file_tree
        ).pack(side="right")

        self.tree_text = ctk.CTkTextbox(
            right_frame,
            height=150,
            font=ctk.CTkFont(family="Consolas", size=11)
        )
        self.tree_text.pack(fill="both", expand=True, padx=5, pady=(0, 10))
        self.tree_text.configure(state="disabled")

        # Bottom - Log
        log_label_frame = ctk.CTkFrame(self)
        log_label_frame.pack(fill="x", padx=10, pady=(0, 5))
        ctk.CTkLabel(
            log_label_frame,
            text="Log",
            font=ctk.CTkFont(weight="bold")
        ).pack(side="left")

        self.log_text = ctk.CTkTextbox(
            self,
            height=120,
            font=ctk.CTkFont(family="Consolas", size=10)
        )
        self.log_text.pack(fill="x", padx=10, pady=(0, 10))
        self.log_text.configure(state="disabled")

    def _log(self, message: str, level: str = "info"):
        """Add a message to the log."""
        timestamp = datetime.now().strftime("%H:%M:%S")
        prefix = ""
        if level == "error":
            prefix = "ERROR "
        elif level == "warning":
            prefix = "WARN  "

        self.log_text.configure(state="normal")
        self.log_text.insert("end", f"{timestamp}  {prefix}{message}\n")
        self.log_text.see("end")
        self.log_text.configure(state="disabled")

    def _init_project(self, path: str):
        """Initialize project with given path."""
        if not os.path.isdir(path):
            self._log(f"Verzeichnis existiert nicht: {path}", "error")
            return False

        self.backup_manager = BackupManager(path)
        self.executor = DCTPExecutor(
            path,
            self.backup_manager,
            auto_renumber=self.settings.get("auto_renumber", True),
            validate_checksums=self.settings.get("validate_checksum", True)
        )
        self.settings["project_path"] = path
        self._save_settings()
        self._log(f"Projekt geladen: {path}")
        self._refresh_file_tree()
        return True

    def _browse_project(self):
        """Open directory browser for project selection."""
        initial_dir = self.project_entry.get() or os.path.expanduser("~")
        path = filedialog.askdirectory(initialdir=initial_dir)
        if path:
            self.project_entry.delete(0, "end")
            self.project_entry.insert(0, path)
            self._init_project(path)

    def _open_settings(self):
        """Open settings dialog."""
        dialog = SettingsDialog(self, self.settings)
        self.wait_window(dialog)

        if dialog.result:
            old_theme = self.settings.get("theme")
            self.settings.update(dialog.result)
            self._save_settings()

            # Apply theme change
            if dialog.result.get("theme") != old_theme:
                ctk.set_appearance_mode(dialog.result["theme"])

            # Reinitialize project with new settings
            if self.settings.get("project_path"):
                self._init_project(self.settings["project_path"])

            self._log("Einstellungen gespeichert")

    def _analyze(self):
        """Analyze input and show preview."""
        # Ensure project is initialized
        project_path = self.project_entry.get()
        if not project_path:
            messagebox.showerror("Fehler", "Bitte waehle ein Projektverzeichnis")
            return

        if not self.executor or self.settings.get("project_path") != project_path:
            if not self._init_project(project_path):
                return

        # Get input
        input_text = self.input_text.get("1.0", "end-1c")
        if not input_text.strip():
            self._log("Kein Input vorhanden", "warning")
            return

        # Parse
        self._log("Analysiere...")
        result = self.parser.parse(input_text)

        # Handle errors
        if result.has_errors:
            self._log(f"{len(result.errors)} Parse-Fehler gefunden", "error")
            for error in result.errors:
                self._log(f"  Zeile {error.line_number}: {error.message}", "error")
            return

        if not result.operations:
            self._log("Keine Operationen gefunden", "warning")
            return

        self.current_operations = result.operations
        self._log(f"{len(result.operations)} Operationen gefunden")

        # Generate previews
        self.current_previews = self.executor.preview(result.operations)

        # Display previews
        self._display_previews()

        # Enable execute button
        self.execute_btn.configure(state="normal")

    def _display_previews(self):
        """Display operation previews."""
        self.preview_text.configure(state="normal")
        self.preview_text.delete("1.0", "end")

        self.diff_text.configure(state="normal")
        self.diff_text.delete("1.0", "end")

        for preview in self.current_previews:
            # Add to preview list
            self.preview_text.insert("end", f"{preview.description}\n")
            for warning in preview.warnings:
                self.preview_text.insert("end", f"  WARNING {warning}\n")

            # Add diff if available
            if preview.diff and preview.diff.has_changes:
                self.diff_text.insert("end", f"--- {preview.diff.filename} ---\n")
                for line in preview.diff.lines:
                    if line.type == DiffType.ADDED:
                        self.diff_text.insert("end", f"+ {line.content}\n")
                    elif line.type == DiffType.REMOVED:
                        self.diff_text.insert("end", f"- {line.content}\n")
                    elif line.type == DiffType.UNCHANGED:
                        self.diff_text.insert("end", f"  {line.content}\n")
                self.diff_text.insert("end", "\n")

        self.preview_text.configure(state="disabled")
        self.diff_text.configure(state="disabled")

    def _execute(self):
        """Execute the analyzed operations."""
        if not self.current_operations:
            self._log("Keine Operationen zum Ausfuehren", "warning")
            return

        if not self.executor:
            self._log("Kein Projekt initialisiert", "error")
            return

        # Confirm
        count = len(self.current_operations)
        if not messagebox.askyesno(
            "Bestaetigen",
            f"{count} Operationen ausfuehren?"
        ):
            return

        self._log(f"Fuehre {count} Operationen aus...")

        # Execute
        results = self.executor.execute(self.current_operations)

        # Log results
        success_count = 0
        for result in results:
            if result.status == ResultStatus.SUCCESS:
                self._log(f"OK {result.message}")
                success_count += 1
            elif result.status == ResultStatus.WARNING:
                self._log(f"WARN {result.message}", "warning")
            elif result.status == ResultStatus.ERROR:
                self._log(f"ERROR {result.message}", "error")

        self._log(f"Abgeschlossen: {success_count}/{count} erfolgreich")

        # Clear current operations
        self.current_operations = []
        self.current_previews = []
        self.execute_btn.configure(state="disabled")

        # Refresh file tree
        self._refresh_file_tree()

    def _undo(self):
        """Undo last operation."""
        if not self.backup_manager:
            self._log("Kein Projekt initialisiert", "error")
            return

        success, restored = self.backup_manager.restore_last()

        if success:
            self._log(f"Undo erfolgreich: {len(restored)} Dateien wiederhergestellt")
            for f in restored:
                self._log(f"  -> {f}")
            self._refresh_file_tree()
        else:
            self._log("Kein Backup zum Wiederherstellen", "warning")

    def _clear(self):
        """Clear input and preview areas."""
        self.input_text.delete("1.0", "end")

        self.preview_text.configure(state="normal")
        self.preview_text.delete("1.0", "end")
        self.preview_text.configure(state="disabled")

        self.diff_text.configure(state="normal")
        self.diff_text.delete("1.0", "end")
        self.diff_text.configure(state="disabled")

        self.current_operations = []
        self.current_previews = []
        self.execute_btn.configure(state="disabled")

        self._log("Eingabe geloescht")

    def _refresh_file_tree(self):
        """Refresh the file tree display."""
        self.tree_text.configure(state="normal")
        self.tree_text.delete("1.0", "end")

        project_path = self.project_entry.get()
        if not project_path or not os.path.isdir(project_path):
            self.tree_text.insert("end", "(Kein Projekt geladen)")
            self.tree_text.configure(state="disabled")
            return

        # Build simple tree
        try:
            self._add_tree_items(Path(project_path), 0)
        except Exception as e:
            self.tree_text.insert("end", f"Fehler: {e}")

        self.tree_text.configure(state="disabled")

    def _add_tree_items(self, path: Path, level: int, max_items: int = 100):
        """Recursively add items to tree display."""
        if level > 5:  # Limit depth
            return

        indent = "  " * level

        try:
            items = sorted(path.iterdir(), key=lambda x: (not x.is_dir(), x.name.lower()))
            count = 0

            for item in items:
                if count >= max_items:
                    self.tree_text.insert("end", f"{indent}  ... (mehr Dateien)\n")
                    break

                # Skip hidden files and backup directory
                if item.name.startswith('.'):
                    continue

                if item.is_dir():
                    self.tree_text.insert("end", f"{indent}DIR {item.name}/\n")
                    self._add_tree_items(item, level + 1, max_items=20)
                else:
                    self.tree_text.insert("end", f"{indent}FILE {item.name}\n")

                count += 1

        except PermissionError:
            self.tree_text.insert("end", f"{indent}  (Zugriff verweigert)\n")


def main():
    """Main entry point."""
    app = DCTPApp()
    app.mainloop()


if __name__ == "__main__":
    main()
