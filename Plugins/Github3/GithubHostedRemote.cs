﻿using GitUIPluginInterfaces.RepositoryHosts;

namespace Github3
{
    internal class GithubHostedRemote : IHostedRemote
    {
        private GithubRepo _repo;
        public GithubHostedRemote(string name, string owner, string remoteRepositoryName)
        {
            Name = name;
            Owner = owner;
            RemoteRepositoryName = remoteRepositoryName;
        }

        public IHostedRepository GetHostedRepository()
        {
            if (_repo == null)
            {
                _repo = new GithubRepo(Github3Plugin.github.getRepository(Owner, RemoteRepositoryName));
            }

            return _repo;
        }

        /// <summary>
        /// Local name of the remote, 'origin'
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Owner of the remote repository, in
        /// git@github.com:mabako/Git.hub.git this is 'mabako'
        /// </summary>
        public string Owner { get; }

        /// <summary>
        /// Name of the remote repository, in
        /// git@github.com:mabako/Git.hub.git this is 'Git.hub'
        /// </summary>
        public string RemoteRepositoryName { get; }

        public string Data => Owner + "/" + RemoteRepositoryName;
        public string DisplayData => Data;
        public bool IsOwnedByMe => GithubLoginInfo.Username == Owner;
    }
}
