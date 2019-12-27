import os
import json
from argparse import ArgumentParser

CHANNEL_MAP_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'channel_tfm_map.json')

class ChannelMap:
    def __init__(self):
        file_name = CHANNEL_MAP_FILE
        with open(file_name) as map_file:
            map = json.load(map_file)
            self.channels = map['channels']

    def get_channel(self, channel_name):
        for channel in self.channels:
            if channel['name'] == channel_name or channel['status'] == channel_name:
                return channel['name']
        raise Exception('Channel %s is not supported. Supported channels: %s' % (channel_name, self.get_supported_channels()))
    
    def get_channels(self, channel_names:list):
        channels = set()
        for channel in channel_names:
           channels.add(self.get_channel(channel))
        return channels
            
    def get_tfm(self, channel_name:str):
        for channel in self.channels:
            if channel['name'] == channel_name or channel['status'] == channel_name:
                return channel['tfm']
        raise Exception('Channel %s is not supported. Supported channels: %s' % (channel_name, self.get_supported_channels()))

    def get_branch(self, channel_name:str):
        for channel in self.channels:
            if channel['name'] == channel_name or channel['status'] == channel_name:
                return channel['branch']
        raise Exception('Channel %s is not supported. Supported channels: %s' % (channel_name, self.get_supported_channels()))
    
    def get_channel_from_tfm(self, tfm_name:str):
        for channel in self.channels:
            if channel['tfm'] == tfm_name:
                return channel['name']
        raise Exception('Framework %s is not supported. Supported frameworks: %s' % (tfm_name, self.get_supported_frameworks()))

    def get_supported_channels(self):
        names = set()
        for channel in self.channels:
            names.add(channel['status'])
            names.add(channel['name'])
        return names

    def get_supported_frameworks(self):
        names = set()
        for channel in self.channels:
            names.add(channel['tfm'])
        return names

def __main():
    parser = ArgumentParser()
    parser.add_argument('--channel', type=str, required=True, dest='channel', help='channel name or status')
    parser.add_argument('--setup-pipeline', action='store_true', dest='setup_pipeline', help='channel required to set up pipeline variables')
    args = parser.parse_args()

    map = ChannelMap()

    if args.setup_pipeline:
        print('##vso[task.setvariable variable=_Framework]%s' % map.get_tfm(args.channel))

if __name__ == "__main__":
    __main()
