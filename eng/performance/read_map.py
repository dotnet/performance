import json

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

