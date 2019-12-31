from argparse import ArgumentParser

class ChannelMap():
    @staticmethod
    def get_supported_channels() -> list:
        '''List of supported channels.'''
        channels = list(
            ChannelMap.__get_channel_target_framework_moniker_map().keys()
        )
        return channels

    @staticmethod
    def get_supported_frameworks() -> list:
        '''List of supported frameworks'''
        return list(set(ChannelMap.__get_channel_target_framework_moniker_map().values()))

    @staticmethod
    def __get_channel_target_framework_moniker_map() -> dict:
        return {
            'master': 'netcoreapp5.0',
            'release/3.1.2xx': 'netcoreapp3.1',
            'release/3.1.1xx': 'netcoreapp3.1',
            '3.1': 'netcoreapp3.1',
            '3.0': 'netcoreapp3.0',
            '2.2': 'netcoreapp2.2',
            '2.1': 'netcoreapp2.1',
            'release/2.1.6xx': 'netcoreapp2.1',
            # For Full Framework download the LTS for dotnet cli.
            'LTS': 'net461'
        }

    @staticmethod
    def get_branch(channel: str) -> str:
        dct = {
            'master': 'master',
            'release/3.1.2xx': 'release/3.1.2xx',
            'release/3.1.1xx': 'release/3.1.1xx',
            '3.1': 'release/3.1',
            '3.0': 'release/3.0',
            '2.2': 'release/2.2',
            '2.1': 'release/2.1',
            'release/2.1.6xx': 'release/2.1.6xx',
            # For Full Framework download the LTS for dotnet cli.
            'LTS': 'LTS'
        }
        if channel in dct:
            return dct[channel]
        else:
            raise Exception('Branch %s is not mapped in the branch table.' % channel)

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
        dct = ChannelMap.__get_channel_target_framework_moniker_map()
        return dct[channel] \
            if channel in dct \
            else None
    
    @staticmethod
    def get_channel_from_target_framework_moniker(target_framework_moniker: str) -> str:
        '''Translate Target Framework Moniker (TFM) to channel name'''
        map = ChannelMap.__get_channel_target_framework_moniker_map()
        for key in map:
            if map[key] == target_framework_moniker:
                return key
        raise Exception('Framework %s is not supported. Supported frameworks: %s' % (target_framework_moniker, ChannelMap.get_supported_frameworks()))


def __main():
    parser = ArgumentParser()
    parser.add_argument('--channel', type=str, required=True, dest='channel', help='channel name or status')
    parser.add_argument('--setup-pipeline', action='store_true', dest='setup_pipeline', help='channel required to set up pipeline variables')
    args = parser.parse_args()

    map = ChannelMap()

    if args.setup_pipeline:
        print('##vso[task.setvariable variable=_Framework]%s' % map.get_target_framework_moniker(args.channel))

if __name__ == "__main__":
    __main()
