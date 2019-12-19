import json
from argparse import ArgumentParser

class ChannelMap:
    def __init__(self, map_file_name:str):
        with open(map_file_name) as map_file:
            map = json.load(map_file)
            self.channels = map['channels']
    
    def get_tfm(self, channel_name:str):
        for channel in self.channels:
            if channel['name'] == channel_name:
                return channel['tfm']

    def get_branch(self, channel_name:str):
        for channel in self.channels:
            if channel['name'] == channel_name:
                return channel['branch']

    def get_supported_channels(self):
        names = []
        for channel in self.channels:
            names.append(channel['name'])
        return names

def __main():
    parser = ArgumentParser()
    parser.add_argument('--channel', type=str, required=True, dest='channel', help='channel as key to the map')
    parser.add_argument('--file', type=str, required=True, dest='file')
    parser.add_argument('--setup-pipeline', action='store_true', dest='setup_pipeline', help='channel required to set up pipeline variables')
    args = parser.parse_args()

    map = ChannelMap(args.file)

    if args.setup_pipeline:
        print('##vso[task.setvariable variable=framework]%s' % map.get_tfm(args.channel))
        print('##vso[task.setvariable variable=branch]%s' % map.get_branch(args.channel))

if __name__ == "__main__":
    __main()
