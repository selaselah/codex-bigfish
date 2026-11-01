#!/usr/bin/env python3
"""Check actual projected tress motion from the matched Unity review, not solver targets."""
import argparse
import csv
import json
import math
from pathlib import Path


def load(path):
    with path.open() as source:
        rows = list(csv.DictReader(source))
    assert len(rows) == 600, f'{path}: expected 600 matched frames'
    result = []
    for frame, row in enumerate(rows):
        assert int(row['frame']) == frame, f'{path}: missing/reordered frame'
        values = [[float(row[f'g{g}{axis}']) for axis in 'xyz'] for g in range(5)]
        assert all(math.isfinite(v) for group in values for v in group), path
        result.append(values)
    return result


def correlation(a, b):
    ma, mb = sum(a) / len(a), sum(b) / len(b)
    aa, bb = [v - ma for v in a], [v - mb for v in b]
    denominator = math.sqrt(sum(v*v for v in aa) * sum(v*v for v in bb))
    assert denominator > 1e-12, 'a rigid/stationary series cannot establish cohesion'
    return sum(x*y for x, y in zip(aa, bb)) / denominator


def summarize(samples):
    report = {}
    for name, first, last in [('work_stop', 30, 420), ('bow_settle', 420, 600), ('all', 30, 600)]:
        frames = samples[first:last]
        mean = [[sum(g[axis] for g in frame) / 5 for axis in range(3)] for frame in frames]
        spread = math.sqrt(sum(sum((g[axis]-m[axis])**2 for axis in range(3))
                               for frame, m in zip(frames, mean) for g in frame) / (5*len(frames)))
        amplitude = math.sqrt(sum(sum(v*v for v in m) for m in mean) / len(frames))
        assert amplitude > 1e-4, 'lost the common inertial motion'
        report[name] = {'rms_normalized_spread': spread, 'rms_normalized_common_motion': amplitude,
                        'spread_to_motion': spread / amplitude}
    x = [[frame[g][0] for frame in samples[30:420]] for g in range(5)]
    report['x_correlation_work_stop'] = [[correlation(a, b) for b in x] for a in x]
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--folder', type=Path, required=True, help='Unity review motion folder')
    args = parser.parse_args()
    reports = {version: summarize(load(args.folder / f'{version}-cohesion.csv')) for version in ('before', 'after')}
    ratio = reports['after']['work_stop']['rms_normalized_spread'] / reports['before']['work_stop']['rms_normalized_spread']
    minimum = min(reports['after']['x_correlation_work_stop'][i][j] for i in range(5) for j in range(i+1, 5))
    assert ratio < .55, f'projected motion remains scattered: {ratio}'
    assert minimum > .98, f'projected tresses still disagree in lateral timing: {minimum}'
    reports.update(work_stop_spread_ratio=ratio, minimum_work_stop_x_correlation=minimum,
                   scope='Five actual post-projection middle bands in mesh space, before skinning, in the fixed matched sequence; not all poses or collision proof.')
    (args.folder.parent / 'Cohesion-Validation.json').write_text(json.dumps(reports, indent=2) + '\n')
    print(f'Projected tress cohesion passed: spread ratio {ratio:.6f}, minimum lateral correlation {minimum:.6f}')


if __name__ == '__main__':
    main()
