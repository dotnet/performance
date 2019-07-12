from benchview.utils.common import is_null_or_whitespace
from os import path

import xml.etree.ElementTree as ET

def read_test_data(fileName: str, better: str, **other_args: dict) -> tuple:
    split_separator = other_args['namespace_separator']
    if not path.isfile(fileName):
        raise ValueError('Input xunit XML file "{0}" does not exist.'.format(fileName))
    else:
        SCENARIOBENCHMARK_NODE_PATH = 'ScenarioBenchmark'
        COLLECTION_NODE_PATH = '{0}/Tests'.format(SCENARIOBENCHMARK_NODE_PATH)
        TEST_NODE_PATH = '{0}/Test'.format(COLLECTION_NODE_PATH)
        METRICS_NODE_PATH = '{0}/Performance/metrics'.format(TEST_NODE_PATH)
        ITERATION_NODE_PATH = '{0}/Performance/iterations/iteration'.format(TEST_NODE_PATH)

        # Rather than using ET.parse(...) and looking up nodes using find,
        # findall, or by xpath of some sort, we use the event-based
        # ET.iterparse(...) instead. This is much more efficient on large input
        # files containing hundreds of thousands of nodes, since we don't have
        # to store the entire DOM in memory at once. Although iterparse
        # does construct the DOM as it goes along, we can free resources we no
        # longer need by clearing subtrees of the DOM when we no longer need
        # them (see calls to node.clear() below).

        test_name_str = None
        test_names = None
        metrics = {}
        metric_capture = False
        node_list = []
        node_path = None
        root_namespaces = []
        for event, node in ET.iterparse(fileName, events=['start', 'end']):
            if event == 'start':
                node_list.append(node.tag)
            node_path = '/'.join(node_list)

            # <ScenarioBenchmark> ... </ScenarioBenchmark>
            if event == 'start' and node_path == SCENARIOBENCHMARK_NODE_PATH:
                root_namespace_str = node.get('Namespace')
                if not is_null_or_whitespace(root_namespace_str):
                    root_namespaces = root_namespace_str.split(split_separator) if split_separator is not None else [root_namespace_str]
                root_name_str = node.get('Name')
                if is_null_or_whitespace(root_name_str):
                    raise ValueError('Missing "Name" attribute on "ScenarioBenchmark" element.')
                root_namespaces.append(root_name_str)
            elif event == 'end' and node_path == SCENARIOBENCHMARK_NODE_PATH:
                node.clear()

            # <Tests> ... </Tests>
            elif event == 'start' and node_path == TEST_NODE_PATH:
                # We want to tokenize the test path, but not the leaf name
                # (since it can validly contain the '.' separator as part of its
                # name). The easiest way to do this is to tokenize the 'type'
                # attribute, and consume the unique remainder of 'name' in its
                # entirety as the last token.
                test_namespace_str = node.get('Namespace')
                test_name_str = node.get('Name')
                test_namespaces = [] if is_null_or_whitespace(test_namespace_str) else (test_namespace_str.split(split_separator) if split_separator is not None else [test_namespace_str])
                test_names = root_namespaces + test_namespaces + [test_name_str]
            elif event == 'end' and node_path == TEST_NODE_PATH:
                metrics.clear()
                node.clear()

            # <metrics> ... </metrics>
            elif event == 'start' and node_path == METRICS_NODE_PATH:
                metric_capture = True
            elif event == 'end' and node_path == METRICS_NODE_PATH:
                metric_capture = False
            elif event == 'end' and metric_capture:
                # <MetricId displayName="..." unit="..." />
                metrics[node.tag] = { 'metric': node.get('displayName'), 'unit': node.get('unit'), 'better': better }

            # <iteration index="..." MetricId="..." MetricId2="..." ... />
            elif event == 'end' and node_path == ITERATION_NODE_PATH:
                for m in node.keys():
                    if m in metrics:
                        yield (test_names, node.get(m), metrics[m])

            if event == 'end':
                node_list.pop()
