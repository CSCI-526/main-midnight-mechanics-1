import pandas as pd
import matplotlib.pyplot as plt
import os


top_n = 10   


df = pd.read_excel("Beta Metric #4 .xlsx")
df.columns = df.columns.str.strip()


if "Level" in df.columns:
    df = df[~df["Level"].isin(["Tutorial", "Unknown", "PlayScene"])]


skill_counts = df["Skills Chosen"].value_counts().head(top_n)


plt.figure(figsize=(8,5))
skill_counts.plot(kind="bar", color="mediumpurple", alpha=0.9)

plt.title(f"Metric #4: Top {top_n} Most Selected Skills")
plt.xlabel("Skill Name")
plt.ylabel("Times Selected")
plt.xticks(rotation=45, ha="right")
plt.tight_layout()


save_path = os.path.join(os.path.dirname(__file__), f"metric4_skill_tracker_top{top_n}.png")
plt.savefig(save_path, dpi=300)
print(f"✅ Chart saved at: {save_path}")

plt.show()
