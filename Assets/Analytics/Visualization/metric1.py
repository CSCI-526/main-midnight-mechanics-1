import pandas as pd
import matplotlib.pyplot as plt


# df = pd.read_csv("Metric #1  - Form Responses 1.csv")
df = pd.read_csv("/Users/siddhanthsalian/Downloads/Metric #1  - Form Responses 1.csv")

df_space = df[df["KeyPressed"] == "Space"].copy()

df_space["Hit Success"] = df_space["Hit Success"].astype(str).str.upper() == "TRUE"

tot = df_space.groupby("LevelNumber").size().rename("Total").reset_index()
succ = (df_space[df_space["Hit Success"]]
        .groupby("LevelNumber").size().rename("Success").reset_index())

levels = pd.DataFrame({"LevelNumber": [1, 2, 3, 4, 5]})
acc = levels.merge(tot, on="LevelNumber", how="left") \
            .merge(succ, on="LevelNumber", how="left") \
            .fillna(0)

acc["AccuracyPct"] = (acc["Success"] / acc["Total"]).where(acc["Total"] > 0, 0) * 100

fig, ax = plt.subplots(figsize=(9, 5))

ax.bar(acc["LevelNumber"], acc["AccuracyPct"])

ax.axhline(60, linestyle="--")
ax.axhline(85, linestyle="--")
ax.text(5.15, 30, "Miss < 60%", va="center")
ax.text(5.15, 72.5, "Good 60–85%", va="center")
ax.text(5.15, 92.5, "Perfect ≥ 85%", va="center")
ax.set_title("Beat Accuracy (%) per Level")
ax.set_xlabel("Level")
ax.set_ylabel("Beat Accuracy (%)")
ax.set_xticks([1, 2, 3, 4, 5])
ax.set_ylim(0, 100)
ax.grid(True, axis="y", linewidth=0.4)


for x, y in zip(acc["LevelNumber"], acc["AccuracyPct"].round(1)):
    ax.text(x, max(y, 0) + 2, f"{y:.1f}%", ha="center", va="bottom")

plt.tight_layout()
plt.show()

fig.savefig("beat_accuracy_by_level.png", dpi=200, bbox_inches="tight")