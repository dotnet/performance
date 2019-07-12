from os import path

import xml.etree.ElementTree as ET

def read_test_data(fileName: str, better: str, counter: list, **other_args: dict) -> tuple:
    if not path.isfile(fileName):
        raise ValueError('Input xunit XML file "{0}" does not exist.'.format(fileName))
    else:
        SCENARIO_RESULT_NODE_PATH = 'ScenarioResults/ScenarioResult'
        COUNTER_RESULTS_NODE_PATH = '{0}/CounterResults'.format(SCENARIO_RESULT_NODE_PATH)

        # Rather than using ET.parse(...) and looking up nodes using find,
        # findall, or by xpath of some sort, we use the event-based
        # ET.iterparse(...) instead. This is much more efficient on large input
        # files containing hundreds of thousands of nodes, since we don't have
        # to store the entire DOM in memory at once. Although iterparse
        # does construct the DOM as it goes along, we can free resources we no
        # longer need by clearing subtrees of the DOM when we no longer need
        # them (see calls to node.clear() below).
        
        test_name = None
        node_list = []
        node_path = None
        for event, node in ET.iterparse(fileName, events=['start', 'end']):
            if event == 'start':
                node_list.append(node.tag)
            node_path = '/'.join(node_list)

            # <ScenarioResult> ... </ScenarioResult>
            if event == 'start' and node_path == SCENARIO_RESULT_NODE_PATH:
                test_name_attr = node.get('Name')
                if test_name_attr != '..TestDiagnostics..':
                    test_name = test_name_attr
                else:
                    test_name = None
            elif event == 'end' and node_path == SCENARIO_RESULT_NODE_PATH:
                node.clear()

            # <CounterResults> ... </CounterResults>
            elif test_name is not None and event == 'end' and node_path == COUNTER_RESULTS_NODE_PATH:
                for counter_result in node.getchildren():
                    # Only get <CounterResult>'s, <ListResult> is mixed in here
                    is_counterresult = counter_result.tag == 'CounterResult'
                    
                    # If counter is None, capture all metrics in the file, otherwise check if the metric is in the user specified list
                    should_capture = (counter is None) or (counter_result.get('Name') in counter)
                    
                    if is_counterresult and should_capture:
                        metric_info = { 'metric': counter_result.get('Name'), 'unit': counter_result.get('Units'), 'better': better }
                        value = counter_result.text
                        yield ([test_name], value, metric_info)
                
                node.clear()       

            if event == 'end':
                node_list.pop()
