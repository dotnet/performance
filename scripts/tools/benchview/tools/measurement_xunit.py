from os import path

import xml.etree.ElementTree as ET

def read_test_data(fileName: str, better: str, **other_args: dict) -> tuple:
    if not path.isfile(fileName):
        raise ValueError('Input xunit XML file "{0}" does not exist.'.format(fileName))
    else:
        ASSEMBLY_NODE_PATH = 'assemblies/assembly'
        COLLECTION_NODE_PATH = '{0}/collection'.format(ASSEMBLY_NODE_PATH)
        TEST_NODE_PATH = '{0}/test'.format(COLLECTION_NODE_PATH)
        METRICS_NODE_PATH = '{0}/performance/metrics'.format(TEST_NODE_PATH)
        ITERATION_NODE_PATH = '{0}/performance/iterations/iteration'.format(TEST_NODE_PATH)

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
        for event, node in ET.iterparse(fileName, events=['start', 'end']):
            if event == 'start':
                node_list.append(node.tag)
            node_path = '/'.join(node_list)

            # <test> ... </test>
            if event == 'start' and node_path == TEST_NODE_PATH:
                # We want to tokenize the test path, but not the leaf name
                # (since it can validly contain the '.' separator as part of its
                # name). The easiest way to do this is to tokenize the 'type'
                # attribute, and consume the unique remainder of 'name' in its
                # entirety as the last token.
                test_namespace_str = node.get('type')
                test_name_str = node.get('name').replace('{0}.'.format(test_namespace_str), '', 1)
                test_names = test_namespace_str.split('.') + [test_name_str]
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

            # <collection> ... </collection>
            elif event == 'end' and node_path == COLLECTION_NODE_PATH:
                node.clear()

            # <assembly> ... </assembly>
            elif event == 'end' and node_path == ASSEMBLY_NODE_PATH:
                node.clear()

            if event == 'end':
                node_list.pop()
