# TexCore Score Rules

Use docs/texcore-module-score-data.xlsx as the source of truth for module score configuration.

Separate:

- question/exam marks
- module cumulative points
- coin or account balance
- completion reward

DataClass_Topic.score, haveScore, ExamRecordBase.Mark, and SubmitRecord are question/exam submission mechanisms. They are not by themselves a complete module-points or coin system.

Do not fill scene stepIndex from score-table row numbers. Map score rows to real scene actions first.
