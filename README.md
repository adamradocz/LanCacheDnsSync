# LanCache DNS Sync

LanCache DNS Sync designed to automate the synchronization of LanCache DNS entries. It fetches DNS records daily, used by LanCache, and updates the local DNS server (AdGuard Home) configuration.
It serves users who already have a running local DNS server (AdGuard Home) in their LAN and wish to use that server to resolve DNS queries for LanCache, instead of using the default LanCache-DNS container.

## Usage

Demonstration of how to use LanCache DNS Sync with AdGuard Home and LanCache Monolithic cache server. Adapt the `docker-compose.yml` file to your environment, ensuring that the IP addresses and paths match your setup.

```yaml
services:
  adguardhome:
    image: adguard/adguardhome:latest
    container_name: adguardhome
    ports:
      - 192.168.0.3:3000:3000/tcp # initial setup web interface
      - 192.168.0.3:53:53/tcp # plain dns over tcp
      - 192.168.0.3:53:53/udp # plain dns over udp
      - 192.168.0.3:80:80/tcp # http web interface
      #- 443:443/tcp # Add if you are going to run AdGuard Home as an HTTPS/DNS-over-HTTPS⁠ server.
      #- 443:443/udp # Add if you are going to run AdGuard Home as an HTTPS/DNS-over-HTTPS⁠ server.
    volumes:
      - ${APPDATA_PATH}/AdGuardHome/config:/opt/adguardhome/conf # app configuration
      - ${APPDATA_PATH}/AdGuardHome/work:/opt/adguardhome/work # app working directory
    networks:
      lan-net:
         ipv4_address: 192.168.0.3

  monolithic:
    image: lancachenet/monolithic:latest
    container_name: lancache
    environment:
      - CACHE_DISK_SIZE=${CACHE_DISK_SIZE}
      - CACHE_INDEX_SIZE=${CACHE_INDEX_SIZE}
      - MIN_FREE_DISK=${MIN_FREE_DISK}
      - CACHE_MAX_AGE=${CACHE_MAX_AGE}
      - UPSTREAM_DNS=${UPSTREAM_DNS}
      - CACHE_SLICE_SIZE=${CACHE_SLICE_SIZE}
    ports:
      - 192.168.0.4:80:80/tcp
      - 192.168.0.4:443:443/tcp
    volumes:
      - ${APPDATA_PATH}/LanCache/cache:/data/cache
      - ${APPDATA_PATH}/LanCache/logs:/data/logs
    networks:
      lan-net:
         ipv4_address: 192.168.0.4

# Network configuration
networks:

  # Direct LAN access without NAT.
  lan-net:
    name: lan-net
    driver: ipvlan
    driver_opts:
      parent: enp1s0 # In the driver options the parent must be the physical interface.
    ipam:
      config:
        - subnet: 192.168.0.0/24
          gateway: 192.168.0.1
```

### Environment Variables
| Variable           | Description                                 | Required | Default         |
|--------------------|---------------------------------------------|----------|-----------------|
| LANCACHE_IPV4      | IP address of your lancache chaching server | Yes      |                 |
| CACHE_DOMAINS_REPO | Password for AdGuard Home                   | Yes      | https://github.com/uklans/cache-domains.git This is the same default repository used by LanCache-DNS. |

### Volumes
| Volume              | Description                                 |
|---------------------|---------------------------------------------|
| /data/cache-domains | Directory where cache-domains will be cloned and updated. |
| /data/userfilters   | Directory where the `lancache.txt` file will be created and updated. Map it to your AdGuard Home user filters directory. |
