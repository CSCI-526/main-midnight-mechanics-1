import pandas as pd
import matplotlib.pyplot as plt
import os
import sys
from typing import Dict, List

#Configuration Constants
INPUT_FILE = "Beta Metric #1 Latest.xlsx"
OUTPUT_FILE = "metric1_beat_accuracy.png"
EXCLUDED_LEVELS = ["Tutorial", "Unknown"]
ZONE_ORDER = ["Perfect", "Good", "Miss"]
ZONE_COLORS = {"Perfect": "green", "Good": "gold", "Miss": "red"}
FIGURE_SIZE = (8, 5)
DPI = 300
REQUIRED_COLUMNS = ["Level", "Hit Zone"]


def load_data(file_path: str) -> pd.DataFrame:
    
    try:
        if not os.path.exists(file_path):
            raise FileNotFoundError(f"Input file not found: {file_path}")
        df = pd.read_excel(file_path)
        if df.empty:
            raise ValueError("Input file is empty")
        return df
    except Exception as e:
        print(f"❌ Error loading file: {e}", file=sys.stderr)
        sys.exit(1)


def validate_columns(df: pd.DataFrame, required: List[str]) -> None:
    
    missing = [col for col in required if col not in df.columns]
    if missing:
        raise ValueError(f"Missing required columns: {missing}")


def prepare_data(df: pd.DataFrame, excluded_levels: List[str]) -> pd.DataFrame:
    
    
    df.columns = df.columns.str.strip()
    
   
    validate_columns(df, REQUIRED_COLUMNS)
    
    # Remove unwanted levels
    if "Level" in df.columns:
        initial_count = len(df)
        df = df[~df["Level"].isin(excluded_levels)]
        filtered_count = len(df)
        if filtered_count == 0:
            raise ValueError("No data remaining after filtering excluded levels")
        print(f"📊 Filtered {initial_count - filtered_count} rows (excluded levels)")
    
    return df


def calculate_percentages(df: pd.DataFrame) -> pd.DataFrame:
    
    
    counts = df.groupby(["Level", "Hit Zone"]).size().unstack(fill_value=0)
    
    
    percent = counts.div(counts.sum(axis=1), axis=0) * 100
    
    return percent


def create_stacked_bar_chart(
    percent: pd.DataFrame,
    zone_order: List[str],
    colors: Dict[str, str],
    figsize: tuple,
    title: str
) -> None:
    
    plt.figure(figsize=figsize)
    
    bottom = None
    for zone in zone_order:
        if zone in percent.columns:
            plt.bar(
                percent.index,
                percent[zone],
                bottom=bottom,
                color=colors.get(zone, "gray"),
                label=zone,
                edgecolor="white",
                linewidth=0.5
            )
            
            if bottom is None:
                bottom = percent[zone].values
            else:
                bottom = bottom + percent[zone].values
    
    plt.title(title, fontsize=14, fontweight="bold")
    plt.xlabel("Level", fontsize=12)
    plt.ylabel("Percentage (%)", fontsize=12)
    plt.legend(loc="upper right", framealpha=0.9)
    plt.grid(axis="y", alpha=0.3, linestyle="--")
    plt.ylim(0, 100)
    plt.tight_layout()


def save_chart(output_path: str, dpi: int = 300) -> None:
    """Save the chart to file."""
    try:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        full_path = os.path.join(script_dir, output_path)
        plt.savefig(full_path, dpi=dpi, bbox_inches="tight")
        print(f"✅ Chart saved as: {full_path}")
    except Exception as e:
        print(f"❌ Error saving chart: {e}", file=sys.stderr)
        sys.exit(1)


def main():
    """Main execution function."""
    
    df = load_data(INPUT_FILE)
    df = prepare_data(df, EXCLUDED_LEVELS)
    
    
    percent = calculate_percentages(df)
    
    
    create_stacked_bar_chart(
        percent,
        ZONE_ORDER,
        ZONE_COLORS,
        FIGURE_SIZE,
        "Metric #1: Beat Accuracy Distribution per Level"
    )
    
    
    save_chart(OUTPUT_FILE, DPI)
    
    
    plt.show()


if __name__ == "__main__":
    main()
