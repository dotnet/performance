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
    
    def get_tfm(self, channel_name:str):
        for channel in self.channels:
            if channel['name'] == channel_name or channel['status'] == channel_name:
                return channel['tfm']
        raise Exception(f'Channel {channel_name} is not supported. Supported channels: {self.get_supported_channels()}')

    def get_branch(self, channel_name:str):
        for channel in self.channels:
            if channel['name'] == channel_name or channel['status'] == channel_name:
                return channel['branch']
        raise Exception(f'Channel {channel_name} is not supported. Supported channels: {self.get_supported_channels()}')    
    
    def get_channel_from_tfm(self, tfm_name:str):
        for channel in self.channels:
            if channel['tfm'] == tfm_name:
                return channel['name']
        raise Exception(f'Framework {tfm_name} is not supported. Supported frameworks: {self.get_supported_tfms()}')

    def get_supported_channels(self):
        names = []
        for channel in self.channels:
            names.append(channel['name'])
        return names

    def get_supported_tfms(self):
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
        print('##vso[task.setvariable variable=framework]%s' % map.get_tfm(args.channel))
        print('##vso[task.setvariable variable=branch]%s' % map.get_branch(args.channel))

if __name__ == "__main__":
    __main()
