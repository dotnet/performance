# Enabling Performance Testing in the Product Repos

## Value Statement

We want performance testing to be consistent across all product repos that we test.
All repos (in particular, coreclr and corefx) should run in the same way with the same benchmarks so that we can easily determine where a regression occured.

## Promises

- A consistent AzDO yaml template in arcade for performance testing that can be used across the product repositories with minimal set up for product repo owners.
- A helix proj file that runs all of the benchmarks and splits them in a sane way

## Success Metric(s)

- Product repositories use the provided yaml template to run all performance testing
- Product repositories do not try to skirt the yaml template to run performance testing only on the tests they care about
- Setup time for adding performance testing is as simple as adding a new job that references the yaml template that we provide

## Procedural

- Combine the yaml we already use and, likely, the yaml that has been worked on in coreclr (why reinvent the wheel), with any additional logic necessary for pulling down and running the performance repository in product repos
  - yaml will need to include any additional parameters/knobs that the product repos may want to turn (ie tiered/non-tiered/minopts, pgo/no-pgo, etc)
  - yaml will need to include all uploading to benchview logic
  - yaml should be as barebones as possbile with most of the work done in the proj file
  - yaml should allow sending of just the performance repo as well as sending the performance repo and the core_root directory (for repos that need to specify that)
- Add a send-perf-to-helix.proj file that will run the benchmarks in a sane way
  - proj file should include most of the running logic
  - proj file should allow for both partitioned runs and non-partitioned runs (there's no real reason for functional testing to run partitioned since they run so quickly, but anything where we are collecting actual performance data should be partitioned)
  - proj file should accept any csproj from the performance repo so that when new benchmark categories are added (ie ml, scenarios, etc), it is simple to start running those new benchmarks
- Add these two templates (the yaml and project) to arcade
- Let the product repo owners, or all of .NET Core Engineering Partners, that the yaml is ready for their use

## Pros

- We control what they run
- It's a clean solution
- It uses arcade the way it is meant to be used (sharing shared infrastructure)
- Product repos can run per-commit and on PR without having to go outside of their repo
- The performance repo does not have to know about the product repos
- We can use scripting that we already have, modify it to allow a supplied corerun, and almost immediately empower the product repos to start performance testing
- We don't have to know anything about the builds of the product repos
- When new repos want to start running performance testing, we don't have to do anything except point them to the template
- We do not have to work around darc/dependency flow
- We do not need to know why the repo triggered a job (ie because coreclr/corefx/etc changed) because the triggers will be in coreclr/corefx
- While we are still on benchview, this obfuscates a lot of the upload logic so that product repos don't have to worry about getting it wrong
- When we move off Benchview, this will allow us to have a seemless experience for product repos

## Cons

- This still could enable the product repos to do their own thing for perf testing (ie by not using our template)
- Scripting might not be quite general enough to work for anyone
- The product repos will have to pull down the performance repo to do work
- We will need some mechanism of knowing at what version of the performance repo the product repos are running
- Will require us to be very precise in how we build it so that it is truly shareable across repos
- Will require some input from product repo owners
- Will require buy in from product repo owners