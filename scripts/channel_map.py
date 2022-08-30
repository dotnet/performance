from argparse import ArgumentParser

class ChannelMap():
    channel_map = {
        'main': {
            'tfm': 'net7.0',
            'branch': '7.0.1xx',
            'quality': 'daily'
        },
        'release/7.0': {
            'tfm': 'net7.0',
            'branch': '7.0',
            'quality': 'daily'
        },
        'release/6.0': {
            'tfm': 'net6.0',
            'branch': '6.0.1xx',
            'quality': 'daily'
        },
        '6.0': {
            'tfm': 'net6.0',
            'branch': '6.0.1xx',
            'quality': 'daily'
        },
        'nativeaot7.0': {
            'tfm': 'nativeaot7.0',
            'branch': '7.0.1xx',
            'quality': 'daily'
        },
        'master': {
            'tfm': 'net6.0',
            'branch': 'master'
        },
        'nativeaot6.0': {
            'tfm': 'nativeaot6.0',
            'branch': '6.0.1xx',
            'quality': 'daily'
        },
        '5.0':{
            'tfm': 'net5.0',
            'branch': '5.0.4xx',
            'quality': 'daily'
        },
        'release/5.0.1xx-rc2':{
            'tfm': 'net5.0',
            'branch': 'release/5.0.1xx-rc2'
        },
        'release/5.0.1xx':{
            'tfm': 'net5.0',
            'branch': 'release/5.0.1xx'
        },
        'release/3.1.3xx':{
            'tfm': 'netcoreapp3.1',
            'branch': 'release/3.1.3xx'
        },
        'release/3.1.2xx': {
            'tfm': 'netcoreapp3.1',
            'branch': 'release/3.1.2xx'
        },
        'release/3.1.1xx': {
            'tfm': 'netcoreapp3.1',
            'branch': 'release/3.1.1xx'
        },
        '3.1': {
            'tfm': 'netcoreapp3.1',
            'branch': '3.1.4xx',
            'quality': 'daily'
        },
        '3.0': {
            'tfm': 'netcoreapp3.0',
            'branch': 'release/3.0'
        },
        'release/2.1.6xx': {
            'tfm': 'netcoreapp2.1',
            'branch': 'release/2.1.6xx'
        },
        '2.1': {
            'tfm': 'netcoreapp2.1',
            'branch': 'release/2.1'
        },
        'LTS': {
            'tfm': 'net461', # For Full Framework download the LTS for dotnet cli.
            'branch': 'LTS'
        },
        'net48': {
            'tfm': 'net48', # For Full Framework download the LTS for dotnet cli.
            'branch': 'LTS'
        }
    }
    @staticmethod
    def get_supported_channels() -> list:
        '''List of supported channels.'''
        return list(ChannelMap.channel_map.keys())

    @staticmethod
    def get_supported_frameworks() -> list:
        '''List of supported frameworks'''
        frameworks = [ChannelMap.channel_map[channel]['tfm'] for channel in ChannelMap.channel_map]
        return set(frameworks)

    @staticmethod
    def get_branch(channel: str) -> str:
        if channel in ChannelMap.channel_map:
            return ChannelMap.channel_map[channel]['branch']
        else:
            raise Exception('Channel %s is not supported. Supported channels %s' % (channel, ChannelMap.get_supported_channels()))

    @staticmethod
    def get_target_framework_monikers(channels: list) -> list:
        '''
        Translates channel names to Target Framework Monikers (TFMs).
        '''
        monikers = [
            ChannelMap.get_target_framework_moniker(channel)
            for channel in channels
        ]
        return list(set(monikers))

    @staticmethod
    def get_target_framework_moniker(channel: str) -> str:
        '''
        Translate channel name to Target Framework Moniker (TFM)
        '''
        if channel in ChannelMap.channel_map:
            return ChannelMap.channel_map[channel]['tfm']
        else:
            raise Exception('Channel %s is not supported. Supported channels %s' % (channel, ChannelMap.get_supported_channels()))

    @staticmethod
    def get_quality_from_channel(channel: str) -> str:
        '''Translate Target Framework Moniker (TFM) to channel name'''
        if 'quality' in ChannelMap.channel_map[channel]:
            return ChannelMap.channel_map[channel]['quality']
        else:
            return None

    @staticmethod
    def get_channel_from_target_framework_moniker(target_framework_moniker: str) -> str:
        '''Translate Target Framework Moniker (TFM) to channel name'''
        for channel in ChannelMap.channel_map:
            if ChannelMap.channel_map[channel]['tfm'] == target_framework_moniker:
                return channel
        raise Exception('Framework %s is not supported. Supported frameworks: %s' % (target_framework_moniker, ChannelMap.get_supported_frameworks()))
