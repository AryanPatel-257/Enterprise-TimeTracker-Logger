# 🚀 Metadata-Driven Enterprise Data Quality Engine

A high-performance standalone desktop application built in **C# .NET 8** designed to ingest, validate, and clean unorganized business data from Excel/CSV formats using dynamic JSON structural mapping specifications.

## 🛠️ Key Features
- **Zero-Code Configuration:** Instantly morphs to handle any client's custom columns or validation rules purely through an external JSON metadata file.
- **Strict Validation Matrix:** Native pipeline layer handling structural email formats, duplicate filters, currency symbol stripping, data-type enforcement, and custom Regex string checks.
- **Automated Quarantine Filter:** Isolates malformed dataset lines into an external `quarantine_records.xlsx` workbook detailing the exact failure reasons, ensuring zero pipeline crashes.
- **Secure SQL Installation Scripts:** Generates executable transaction blocks (`success_migration.sql`) allowing data engineering teams to safely audit and execute migrations.
