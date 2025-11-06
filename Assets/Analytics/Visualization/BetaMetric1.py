import pandas as pd
import matplotlib.pyplot as plt


df = pd.read_excel("Beta Metric #1.xlsx")


df = df.applymap(lambda x: x.lower() if isinstance(x, str) else x)


counts = {
    "Perfect": (df == "perfect").sum().sum(),
    "Good": (df == "good").sum().sum(),
    "Miss": (df == "miss").sum().sum()
}


data = pd.DataFrame(list(counts.items()), columns=["Category", "Count"])

fig, ax = plt.subplots(figsize=(8, 5))
bars = ax.bar(data["Category"], data["Count"], color=["#4CAF50", "#FFC107", "#F44336"])


for bar in bars:
    height = bar.get_height()
    ax.text(bar.get_x() + bar.get_width()/2, height + 5, f"{int(height)}",
            ha="center", va="bottom", fontsize=10, fontweight="bold")

ax.set_title("Hit Result Distribution", fontsize=14, fontweight="bold")
ax.set_xlabel("Hit Category", fontsize=12)
ax.set_ylabel("Number of Hits", fontsize=12)
ax.grid(axis="y", linestyle="--", alpha=0.6)

plt.tight_layout()
plt.show()


fig.savefig("metric_distribution.png", dpi=200, bbox_inches="tight")

print("Counts per category:")
print(data)
