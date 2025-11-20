import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
import os

df = pd.read_excel("Beta Metric #3.xlsx")
df.columns = df.columns.str.strip()
df = df[~df["Level"].isin(["Tutorial", "Unknown", "PlayScene"])]

df["Timestamp"] = pd.to_datetime(df["Timestamp"], errors="coerce")
df = df.sort_values(["Session ID", "Timestamp"])
df["Seconds"] = df.groupby("Session ID")["Timestamp"].transform(lambda x: (x - x.min()).dt.total_seconds())
df["TimeBin"] = (df["Seconds"] // 5) * 5
avg = df.groupby(["Level", "TimeBin"])["View_Count"].mean().reset_index()

pivot = avg.pivot(index="Level", columns="TimeBin", values="View_Count")

plt.figure(figsize=(10,5))
sns.heatmap(pivot, cmap="coolwarm", linewidths=0.3)
plt.title("Metric #3: Viewer Count Heatmap (Level vs Time)")
plt.xlabel("Time Since Level Start (seconds)")
plt.ylabel("Level")
plt.tight_layout()

save_path = os.path.join(os.path.dirname(__file__), "metric3_viewer_heatmap.png")
plt.savefig(save_path, dpi=300)
print(f"✅ Saved as {save_path}")

plt.show()
