import pandas as pd
import matplotlib.pyplot as plt
import os


df = pd.read_excel("Beta Metric #2.xlsx")


df.columns = df.columns.str.strip()


if "Level" in df.columns:
    df = df[~df["Level"].isin(["Tutorial", "Unknown", "PlayScene"])]


counts = df.groupby(["Level", "Hit Zone"]).size().unstack(fill_value=0)


percent = counts.div(counts.sum(axis=1), axis=0) * 100

plt.figure(figsize=(8,5))
bottom = None
colors = ["green", "gold", "red"]

for col, color in zip(["Perfect", "Good", "Miss"], colors):
    if col in percent.columns:
        plt.bar(percent.index, percent[col], bottom=bottom, color=color, label=col)
        bottom = percent[col] if bottom is None else bottom + percent[col]

plt.title("Metric #2: Red Pellet Accuracy Breakdown per Level")
plt.xlabel("Level")
plt.ylabel("Percentage (%)")
plt.legend()
plt.tight_layout()


script_dir = os.path.dirname(os.path.abspath(__file__))
save_path = os.path.join(script_dir, "metric2_red_pellet_accuracy.png")
plt.savefig(save_path, dpi=300)
print(f"✅ Chart saved as: {save_path}")

plt.show()
