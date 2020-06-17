# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from typing import Optional

from .frozen_dict import FrozenDict
from .type_utils import doc_field, with_slots


@doc_field(
    "weight",
    """
When diffing scores for two traces, this Will be multiplied by the percent difference in a metric.
For example, if config A had a metric value of 2 and config B had a metric value of 3,
and weight is 2, the value of the score is 50% * 2 = 100%.

Weights may be negative to indicate that being below par is preferable.

When considering a single trace alone, the percent difference can come from 'par'.
If 'par' is not set, a geometric mean will be used instead.
""",
)
@doc_field("par", "Expected normal value for this metric.")
@with_slots
@dataclass(frozen=True)
class ScoreElement:
    weight: float
    par: Optional[float] = None


# Maps a metric name to par and weight
# Must be a ReadonlyDict so this is hashable
ScoreSpec = FrozenDict[str, ScoreElement]
