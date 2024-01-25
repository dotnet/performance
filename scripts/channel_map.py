from typing import List, Optional, Set

class ChannelMap():
    channel_map = {
        'main': {
            'tfm': 'net9.0',
            'branch': '9.0',
            'quality': 'daily'
        },
        '9.0': {
            'tfm': 'net9.0',
            'branch': '9.0',
            'quality': 'daily'
        },
        'nativeaot9.0': {
            'tfm': 'nativeaot9.0',
            'branch': '9.0',
            'quality': 'daily'
        },
        'net462': {
            'tfm': 'net462',
            'branch': '9.0', # Keep up to date with main for global.json, net4 is a special case
            'quality': 'daily'
        },
        'net48': {
            'tfm': 'net48',
            'branch': '9.0', # Keep up to date with main for global.json, net4 is a special case
            'quality': 'daily'
        },
        '8.0': {
            'tfm': 'net8.0',
            'branch': '8.0',
            'quality': 'daily'
        },
        'release/8.0': {
            'tfm': 'net8.0',
            'branch': '8.0',
            'quality': 'daily'
        },
        'release/8.0-rc2': {
            'tfm': 'net8.0',
            'branch': '8.0-rc2',
            'quality': 'daily'
        },
        'release/8.0-rc1': {
            'tfm': 'net8.0',
            'branch': '8.0-rc1',
            'quality': 'daily'
        },
        '8.0-preview': {
            'tfm': 'net8.0',
            'branch': '8.0',
            'qualtiy': 'preview'
        },
        'release/8.0-preview7': {
            'tfm': 'net8.0',
            'branch': '8.0-preview7',
            'quality': 'daily'
        },
        'release/8.0-preview6': {
            'tfm': 'net8.0',
            'branch': '8.0-preview6',
            'quality': 'daily'
        },
        'release/8.0-preview5': {
            'tfm': 'net8.0',
            'branch': '8.0-preview5',
            'quality': 'daily'
        },
        'release/8.0-preview4': {
            'tfm': 'net8.0',
            'branch': '8.0-preview4',
            'quality': 'daily'
        },
        'release/8.0-preview3': {
            'tfm': 'net8.0',
            'branch': '8.0-preview3',
            'quality': 'daily'
        },
        'release/8.0-preview2': {
            'tfm': 'net8.0',
            'branch': '8.0-preview2',
            'quality': 'daily'
        },
        'release/8.0-preview1': {
            'tfm': 'net8.0',
            'branch': '8.0-preview1',
            'quality': 'daily'
        },
        'nativeaot8.0': {
            'tfm': 'nativeaot8.0',
            'branch': '8.0',
            'quality': 'daily'
        },
        '7.0': {
            'tfm': 'net7.0',
            'branch': '7.0',
            'quality': 'daily'
        },
        'release/7.0-rc2': {
            'tfm': 'net7.0',
            'branch': '7.0-rc2',
            'quality': 'daily'
        },
        'release/7.0-rc1': {
            'tfm': 'net7.0',
            'branch': '7.0-rc1',
            'quality': 'daily'
        },
        'nativeaot7.0': {
            'tfm': 'nativeaot7.0',
            'branch': '7.0.1xx',
            'quality': 'daily'
        },
        'release/7.0': {
            'tfm': 'net7.0',
            'branch': '7.0',
            'quality': 'daily'
        },
        '6.0': {
            'tfm': 'net6.0',
            'branch': '6.0',
            'quality': 'daily'
        },
        'release/6.0': {
            'tfm': 'net6.0',
            'branch': '6.0',
            'quality': 'daily'
        },
        'nativeaot6.0': {
            'tfm': 'nativeaot6.0',
            'branch': '6.0',
            'quality': 'daily'
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
        'master': {
            'tfm': 'net6.0',
            'branch': 'master'
        }
    }
    @staticmethod
    def get_supported_channels() -> List[str]:
        '''List of supported channels.'''
        return list(ChannelMap.channel_map.keys())

    @staticmethod
    def get_supported_frameworks() -> Set[str]:
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
    def get_target_framework_monikers(channels: List[str]) -> List[str]:
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
    def get_quality_from_channel(channel: str) -> Optional[str]:
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
