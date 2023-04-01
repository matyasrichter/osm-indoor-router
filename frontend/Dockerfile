ARG BASE_IMAGE=node:18-alpine

FROM ${BASE_IMAGE} as build

WORKDIR /app

COPY .yarnrc.yml package.json yarn.lock ./

RUN yarn install --immutable

COPY . .

RUN yarn build

FROM ${BASE_IMAGE} as final

WORKDIR /app

COPY --from=build /app/package.json ./package.json
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/build ./build

EXPOSE 3000
ENV NODE_ENV=production

CMD [ "node", "build" ]
